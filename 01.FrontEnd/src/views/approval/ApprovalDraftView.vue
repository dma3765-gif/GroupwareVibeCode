<template>
  <div>
    <h2 style="margin-bottom: 16px;">결재 기안</h2>
    <el-steps :active="step" style="margin-bottom: 24px;">
      <el-step title="양식 선택" />
      <el-step title="내용 작성" />
      <el-step title="결재선 지정" />
    </el-steps>

    <!-- Step 0: 양식 선택 -->
    <el-card v-if="step === 0">
      <el-row :gutter="16">
        <el-col :span="8" v-for="f in forms" :key="f.id">
          <el-card shadow="hover" style="cursor: pointer; margin-bottom: 12px;" @click="selectForm(f)">
            <h4>{{ f.name }}</h4>
            <p style="color: #999; font-size: 12px;">{{ f.description }}</p>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- Step 1: 내용 작성 -->
    <el-card v-if="step === 1">
      <el-form :model="docForm" label-width="120px">
        <el-form-item label="제목">
          <el-input v-model="docForm.title" />
        </el-form-item>
        <template v-for="field in formFields" :key="field.key">
          <el-form-item :label="field.label">
            <el-input v-if="field.type === 'text'" v-model="docForm.fields[field.key]" />
            <el-input-number v-else-if="field.type === 'number'" v-model="docForm.fields[field.key]" />
            <el-date-picker v-else-if="field.type === 'date'" v-model="docForm.fields[field.key]" type="date" value-format="YYYY-MM-DD" />
            <el-input v-else v-model="docForm.fields[field.key]" type="textarea" :rows="3" />
          </el-form-item>
        </template>
        <el-form-item>
          <el-button @click="step = 0" style="margin-right: 8px;">이전</el-button>
          <el-button type="primary" @click="step = 2">다음</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- Step 2: 결재선 -->
    <el-card v-if="step === 2">
      <h4>결재자 선택</h4>
      <el-select v-model="selectedApprover" filterable placeholder="사용자 검색" style="width: 300px; margin-right: 8px;" @change="addApprover">
        <el-option v-for="u in users" :key="u.id" :label="u.name + ' (' + u.departmentName + ')'" :value="u.id" />
      </el-select>
      <el-tag v-for="(a, i) in approvalLine" :key="i" closable @close="approvalLine.splice(i,1)" style="margin: 4px;">
        {{ i + 1 }}. {{ a.name }}
      </el-tag>
      <div style="margin-top: 16px;">
        <el-button @click="step = 1" style="margin-right: 8px;">이전</el-button>
        <el-button type="primary" @click="saveDraft" :loading="saving">임시저장</el-button>
        <el-button type="success" @click="submit" :loading="saving">제출</el-button>
      </div>
    </el-card>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
const router = useRouter()
const step = ref(0); const forms = ref<Record<string,unknown>[]>([]); const users = ref<Record<string,unknown>[]>([]); const saving = ref(false)
const selectedForm = ref<Record<string,unknown> | null>(null); const formFields = ref<{key:string;label:string;type:string}[]>([])
const docForm = ref({ title: '', formId: '', fields: {} as Record<string,unknown> })
const selectedApprover = ref(''); const approvalLine = ref<{id:string;name:string}[]>([])
async function loadForms() { try { const r = await apiClient.get('/approval/forms', { params: { page:1, pageSize:50 } }); forms.value = r.data.data?.items || [] } catch { /**/ } }
async function loadUsers() { try { const r = await apiClient.get('/users', { params: { page:1, pageSize:100 } }); users.value = r.data.data?.items || [] } catch { /**/ } }
function selectForm(f: Record<string,unknown>) {
  selectedForm.value = f; docForm.value.formId = f.id as string
  try { const schema = typeof f.formSchema === 'string' ? JSON.parse(f.formSchema as string) : f.formSchema; formFields.value = schema?.fields || [] } catch { formFields.value = [] }
  step.value = 1
}
function addApprover(id: string) {
  const u = users.value.find(u => u.id === id) as Record<string,unknown>
  if (u && !approvalLine.value.find(a => a.id === id)) approvalLine.value.push({ id, name: u.name as string })
  selectedApprover.value = ''
}
async function saveDraft() {
  saving.value = true
  try {
    const payload = { ...docForm.value, approvalLine: approvalLine.value.map((a, i) => ({ userId: a.id, order: i+1 })) }
    await apiClient.post('/approval/documents/draft', payload)
    ElMessage.success('임시저장되었습니다'); router.push('/approval')
  } catch { ElMessage.error('저장 실패') } finally { saving.value = false }
}
async function submit() {
  saving.value = true
  try {
    const payload = { ...docForm.value, approvalLine: approvalLine.value.map((a, i) => ({ userId: a.id, order: i+1 })) }
    const r = await apiClient.post('/approval/documents/draft', payload)
    await apiClient.post(`/approval/documents/${r.data.data.id}/submit`)
    ElMessage.success('제출되었습니다'); router.push('/approval')
  } catch { ElMessage.error('제출 실패') } finally { saving.value = false }
}
onMounted(() => { loadForms(); loadUsers() })
</script>
