<template>
  <div v-if="doc" class="approval-detail">
    <!-- 헤더 -->
    <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:16px;">
      <div>
        <el-tag :type="statusTagType" style="margin-right:8px;">{{ doc.statusLabel }}</el-tag>
        <span style="font-size:13px; color:#999;">{{ doc.documentNo }}</span>
      </div>
      <div>
        <el-button @click="$router.back()">목록</el-button>
        <el-button v-if="canRecall" type="warning" @click="handleRecall">회수</el-button>
      </div>
    </div>

    <!-- 결재 도장형 결재선 -->
    <el-card style="margin-bottom:16px;">
      <template #header><span>결재선</span></template>
      <div class="approval-stamps">
        <div
          v-for="item in doc.approvalLine"
          :key="item.seq"
          class="stamp-box"
          :class="'stamp-' + item.status.toLowerCase()"
        >
          <div class="stamp-role">{{ roleLabel(item.role) }}</div>
          <div class="stamp-name">{{ item.name }}</div>
          <div class="stamp-dept" style="font-size:11px; color:#999;">{{ item.departmentName }}</div>
          <div class="stamp-position" style="font-size:11px;">{{ item.positionName }}</div>
          <div v-if="item.actedAt" class="stamp-date" style="font-size:11px; color:#666;">
            {{ formatDate(item.actedAt) }}
          </div>
          <div v-if="item.comment" class="stamp-comment" style="font-size:11px; color:#e00;">{{ item.comment }}</div>
        </div>
      </div>
    </el-card>

    <!-- 문서 정보 -->
    <el-card style="margin-bottom:16px;">
      <template #header>
        <span>{{ doc.formName }} - {{ doc.title }}</span>
      </template>
      <el-descriptions :column="3" border size="small">
        <el-descriptions-item label="기안자">{{ doc.drafter.name }}</el-descriptions-item>
        <el-descriptions-item label="소속">{{ doc.drafter.departmentName }}</el-descriptions-item>
        <el-descriptions-item label="직위">{{ doc.drafter.positionName }}</el-descriptions-item>
        <el-descriptions-item label="기안일">{{ formatDate(doc.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="상신일">{{ doc.submittedAt ? formatDate(doc.submittedAt) : '-' }}</el-descriptions-item>
        <el-descriptions-item label="완료일">{{ doc.completedAt ? formatDate(doc.completedAt) : '-' }}</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 양식 데이터 -->
    <el-card style="margin-bottom:16px;">
      <template #header><span>문서 내용</span></template>
      <el-descriptions :column="2" border size="small">
        <el-descriptions-item
          v-for="(val, key) in doc.formData"
          :key="key"
          :label="String(key)"
        >{{ val }}</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 결재 처리 -->
    <el-card v-if="canProcess" style="margin-bottom:16px;">
      <template #header><span>결재 처리</span></template>
      <el-form :model="actionForm" label-width="80px">
        <el-form-item label="의견">
          <el-input v-model="actionForm.comment" type="textarea" :rows="3" placeholder="의견을 입력하세요 (선택)" />
        </el-form-item>
        <el-form-item v-if="isConsultation" label="협의결과">
          <el-radio-group v-model="actionForm.consultAgree">
            <el-radio :value="true">동의</el-radio>
            <el-radio :value="false">이의</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="isRejecting" label="반려사유">
          <el-input v-model="actionForm.rejectReason" type="textarea" :rows="2" placeholder="반려 사유를 입력하세요 (필수)" />
        </el-form-item>
        <el-form-item>
          <el-button v-if="canApprove" type="success" :loading="processing" @click="handleApprove">
            {{ canFinalApprove ? '전결 승인' : '승인' }}
          </el-button>
          <el-button v-if="canAgree" type="primary" :loading="processing" @click="handleAgree">합의 승인</el-button>
          <el-button v-if="isConsultation" type="info" :loading="processing" @click="handleConsult">협의 처리</el-button>
          <el-button type="danger" :loading="processing" @click="isRejecting = !isRejecting">
            {{ isRejecting ? '반려 취소' : '반려' }}
          </el-button>
          <el-button v-if="isRejecting" type="danger" :loading="processing" @click="handleReject">반려 확인</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 결재 이력 -->
    <el-card>
      <template #header><span>결재 이력</span></template>
      <el-timeline>
        <el-timeline-item
          v-for="(h, i) in doc.histories"
          :key="i"
          :timestamp="formatDate(h.actedAt)"
          placement="top"
        >
          <strong>{{ actionLabel(h.action) }}</strong>
          <span style="margin-left:8px; color:#666;">{{ h.actorName }}</span>
          <div v-if="h.remark" style="font-size:13px; color:#e00; margin-top:4px;">{{ h.remark }}</div>
        </el-timeline-item>
      </el-timeline>
    </el-card>
  </div>

  <div v-else-if="loading" style="text-align:center; padding:40px;">
    <el-icon class="is-loading" :size="32"><Loading /></el-icon>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import apiClient from '@/shared/api/apiClient'
import { useAuthStore } from '@/stores/auth'

interface ApprovalLineItem {
  seq: number
  role: string
  userId: string
  name: string
  departmentName: string
  positionName: string
  status: string
  actedAt?: string
  comment?: string
  isFinalApprovalAllowed?: boolean
}

interface ApprovalDocument {
  id: string
  documentNo: string
  title: string
  formName: string
  formData: Record<string, unknown>
  drafter: { userId: string; name: string; departmentName: string; positionName: string }
  status: string
  statusLabel: string
  approvalLine: ApprovalLineItem[]
  histories: { action: string; actorUserId: string; actorName: string; remark?: string; actedAt: string }[]
  createdAt: string
  submittedAt?: string
  completedAt?: string
}

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const doc = ref<ApprovalDocument | null>(null)
const loading = ref(false)
const processing = ref(false)
const isRejecting = ref(false)

const actionForm = ref({
  comment: '',
  rejectReason: '',
  consultAgree: true,
})

const myUserId = computed(() => authStore.user?.userId ?? '')

// 현재 내가 처리해야 할 결재선 항목
const myPendingLine = computed(() =>
  doc.value?.approvalLine.find(l => l.userId === myUserId.value && l.status === 'Pending')
)

const canProcess = computed(() => !!myPendingLine.value &&
  (doc.value?.status === 'InApproval' || doc.value?.status === 'InAgreement' || doc.value?.status === 'InConsultation' || doc.value?.status === 'Submitted'))

const canApprove = computed(() =>
  myPendingLine.value?.role === 'Approval')

const canFinalApprove = computed(() =>
  myPendingLine.value?.role === 'Approval' && myPendingLine.value?.isFinalApprovalAllowed)

const canAgree = computed(() =>
  myPendingLine.value?.role === 'Agreement')

const isConsultation = computed(() =>
  myPendingLine.value?.role === 'Consultation')

const canRecall = computed(() =>
  doc.value?.drafter.userId === myUserId.value &&
  (doc.value?.status === 'Draft' || doc.value?.status === 'Submitted'))

const statusTagType = computed(() => {
  const s = doc.value?.status
  if (s === 'Completed') return 'success'
  if (s === 'Rejected') return 'danger'
  if (s === 'InApproval' || s === 'InAgreement') return 'warning'
  if (s === 'Draft') return 'info'
  return ''
})

function roleLabel(role: string) {
  const map: Record<string, string> = {
    Approval: '결재', Agreement: '합의', Consultation: '협의',
    ReferenceBefore: '전참조', ReferenceAfter: '후참조', Receiver: '수신',
  }
  return map[role] ?? role
}

function actionLabel(action: string) {
  const map: Record<string, string> = {
    SUBMIT: '상신', APPROVE: '승인', AGREE: '합의승인', CONSULT_AGREE: '협의동의',
    CONSULT_OBJECT: '협의이의', REJECT: '반려', RECALL: '회수',
    FINAL_APPROVE: '전결승인', DELEGATE_APPROVE: '대결처리',
  }
  return map[action] ?? action
}

function formatDate(d: string) {
  return new Date(d).toLocaleString('ko-KR', { timeZone: 'Asia/Seoul' })
}

async function load() {
  loading.value = true
  try {
    const id = route.params.id as string
    const r = await apiClient.get(`/approval-documents/${id}`)
    doc.value = r.data.data
  } catch {
    ElMessage.error('문서를 불러올 수 없습니다.')
    router.back()
  } finally {
    loading.value = false
  }
}

async function handleApprove() {
  if (canFinalApprove.value) {
    await ElMessageBox.confirm('전결 승인하시겠습니까? 이후 결재자는 모두 생략됩니다.', '전결 확인', { type: 'warning' })
    await processAction('final-approve', { comment: actionForm.value.comment })
  } else {
    await processAction('approve', { comment: actionForm.value.comment })
  }
}

async function handleAgree() {
  await processAction('agree', { comment: actionForm.value.comment })
}

async function handleConsult() {
  await processAction('consult', { agree: actionForm.value.consultAgree, comment: actionForm.value.comment })
}

async function handleReject() {
  if (!actionForm.value.rejectReason.trim()) {
    ElMessage.warning('반려 사유를 입력해주세요.')
    return
  }
  await ElMessageBox.confirm('반려하시겠습니까?', '반려 확인', { type: 'warning' })
  await processAction('reject', { reason: actionForm.value.rejectReason })
}

async function handleRecall() {
  await ElMessageBox.confirm('문서를 회수하시겠습니까?', '회수 확인', { type: 'warning' })
  await processAction('recall', {})
}

async function processAction(action: string, payload: Record<string, unknown>) {
  processing.value = true
  try {
    const id = route.params.id as string
    await apiClient.post(`/approval-documents/${id}/${action}`, payload)
    ElMessage.success('처리되었습니다.')
    isRejecting.value = false
    actionForm.value.comment = ''
    actionForm.value.rejectReason = ''
    await load()
  } catch {
    ElMessage.error('처리에 실패했습니다.')
  } finally {
    processing.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.approval-stamps {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.stamp-box {
  border: 2px solid #d0d0d0;
  border-radius: 4px;
  padding: 8px 12px;
  min-width: 90px;
  text-align: center;
  transition: border-color 0.2s;
}

.stamp-role {
  font-size: 11px;
  color: #999;
  border-bottom: 1px solid #eee;
  margin-bottom: 4px;
  padding-bottom: 4px;
}

.stamp-name {
  font-size: 15px;
  font-weight: bold;
  margin: 4px 0;
}

.stamp-approved { border-color: #2196f3; }
.stamp-agreed { border-color: #4caf50; }
.stamp-rejected { border-color: #f44336; background: #fff0f0; }
.stamp-pending { border-color: #ff9800; }
.stamp-skipped { border-color: #ccc; opacity: 0.5; }
</style>
