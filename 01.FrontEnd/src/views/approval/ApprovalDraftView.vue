<template>
  <div>
    <h2 style="margin-bottom: 16px;">결재 기안</h2>
    <el-steps :active="step" style="margin-bottom: 24px;" finish-status="success">
      <el-step title="양식 선택" />
      <el-step title="내용 작성" />
      <el-step title="결재선 지정" />
    </el-steps>

    <!-- Step 0: 양식 선택 -->
    <el-card v-if="step === 0">
      <el-row :gutter="16">
        <el-col v-if="forms.length === 0" :span="24" style="text-align:center; color:#999; padding:24px;">
          사용 가능한 양식이 없습니다.
        </el-col>
        <el-col :span="8" v-for="f in forms" :key="f.id">
          <el-card shadow="hover" style="cursor: pointer; margin-bottom: 12px;" @click="selectForm(f)">
            <h4 style="margin:0 0 4px;">{{ f.name }}</h4>
            <p style="color: #999; font-size: 12px; margin:0;">{{ f.category ?? '일반' }}</p>
            <p v-if="f.description" style="color:#bbb; font-size:12px; margin:4px 0 0;">{{ f.description }}</p>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- Step 1: 내용 작성 -->
    <el-card v-if="step === 1">
      <el-form :model="docForm" label-width="120px">
        <el-form-item label="제목" required>
          <el-input v-model="docForm.title" placeholder="문서 제목을 입력하세요" />
        </el-form-item>
        <DynamicFormRenderer
          v-if="parsedSchema"
          :schema="parsedSchema"
          v-model="docForm.fields"
        />
      </el-form>
      <div style="text-align:right; margin-top:16px;">
        <el-button @click="step = 0">이전</el-button>
        <el-button type="primary" @click="step = 2" :disabled="!docForm.title">다음</el-button>
      </div>
    </el-card>

    <!-- Step 2: 결재선 지정 -->
    <el-card v-if="step === 2">
      <h4 style="margin:0 0 16px;">결재선 지정</h4>
      <ApprovalLineEditor v-model="approvalLine" />
      <el-divider />
      <el-row :gutter="16">
        <el-col :span="12">
          <h5>전참조 (상신 시 알림)</h5>
          <UserPicker v-model="referencesBefore" />
        </el-col>
        <el-col :span="12">
          <h5>후참조 (완료 후 알림)</h5>
          <UserPicker v-model="referencesAfter" />
        </el-col>
      </el-row>
      <el-divider />
      <h5>첨부파일</h5>
      <FileUploader v-model="attachments" entity-type="ApprovalDocument" />
      <div style="text-align:right; margin-top:16px;">
        <el-button @click="step = 1">이전</el-button>
        <el-button @click="saveDraft" :loading="saving">임시저장</el-button>
        <el-button type="primary" @click="submitDoc" :loading="saving" :disabled="approvalLine.length === 0">상신</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
import DynamicFormRenderer from '@/shared/components/DynamicFormRenderer.vue'
import ApprovalLineEditor from '@/shared/components/ApprovalLineEditor.vue'
import UserPicker from '@/shared/components/UserPicker.vue'
import FileUploader from '@/shared/components/FileUploader.vue'
import type { ApprovalLineItem } from '@/shared/components/ApprovalLineEditor.vue'
import type { PickedUser } from '@/shared/components/UserPicker.vue'
import type { UploadedFile } from '@/shared/components/FileUploader.vue'
import type { FormSchema } from '@/shared/components/DynamicFormRenderer.vue'

const router = useRouter()
const route = useRoute()

const step = ref(0)
const forms = ref<Record<string, unknown>[]>([])
const saving = ref(false)
const draftId = ref<string | null>((route.params.id as string) || null)

const parsedSchema = ref<FormSchema | null>(null)
const docForm = ref({ title: '', formId: '', fields: {} as Record<string, unknown> })
const approvalLine = ref<ApprovalLineItem[]>([])
const referencesBefore = ref<PickedUser[]>([])
const referencesAfter = ref<PickedUser[]>([])
const attachments = ref<UploadedFile[]>([])

async function loadForms() {
  try {
    const r = await apiClient.get('/approval-forms', { params: { page: 1, pageSize: 50 } })
    forms.value = r.data.data?.items || []
  } catch { /**/ }
}

async function loadDraft() {
  if (!draftId.value) return
  try {
    const r = await apiClient.get(`/approval-documents/${draftId.value}`)
    const doc = r.data.data
    docForm.value.title = doc.title
    docForm.value.formId = doc.formId
    docForm.value.fields = doc.formData || {}
    approvalLine.value = doc.approvalLine?.map((l: Record<string, unknown>) => ({
      seq: l.seq as number,
      role: l.role as string,
      userId: l.userId as string,
      name: l.name as string,
      departmentName: l.departmentName as string,
      positionName: l.positionName as string,
      isFinalApprovalAllowed: l.isFinalApprovalAllowed as boolean,
    })) || []
    const form = forms.value.find(f => f.id === doc.formId)
    if (form) applySchema(form)
    step.value = 2
  } catch { /**/ }
}

function selectForm(f: Record<string, unknown>) {
  docForm.value.formId = f.id as string
  applySchema(f)
  step.value = 1
}

function applySchema(f: Record<string, unknown>) {
  try {
    parsedSchema.value = typeof f.formSchema === 'string'
      ? JSON.parse(f.formSchema as string)
      : (f.formSchema as FormSchema) || null
  } catch { parsedSchema.value = null }
}

function buildPayload() {
  return {
    formId: docForm.value.formId,
    title: docForm.value.title,
    formData: docForm.value.fields,
    approvalLine: approvalLine.value.map(a => ({
      seq: a.seq,
      role: a.role,
      userId: a.userId,
      isFinalApprovalAllowed: a.isFinalApprovalAllowed ?? false,
    })),
    referenceBeforeUserIds: referencesBefore.value.map(u => u.id),
    referenceAfterUserIds: referencesAfter.value.map(u => u.id),
  }
}

async function saveDraft() {
  saving.value = true
  try {
    const r = await apiClient.post('/approval-documents', buildPayload())
    draftId.value = r.data.data?.id
    ElMessage.success('임시저장되었습니다.')
  } catch { ElMessage.error('저장에 실패했습니다.') }
  finally { saving.value = false }
}

async function submitDoc() {
  saving.value = true
  try {
    let id = draftId.value
    if (!id) {
      const r = await apiClient.post('/approval-documents', buildPayload())
      id = r.data.data?.id
    }
    await apiClient.post(`/approval-documents/${id}/submit`)
    ElMessage.success('상신되었습니다.')
    router.push('/approval')
  } catch { ElMessage.error('상신에 실패했습니다.') }
  finally { saving.value = false }
}

onMounted(async () => {
  await loadForms()
  if (draftId.value) await loadDraft()
})
</script>
