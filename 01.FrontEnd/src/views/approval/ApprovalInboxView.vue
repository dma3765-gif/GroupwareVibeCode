<template>
  <div>
    <h2 style="margin-bottom: 16px;">전자결재</h2>
    <el-tabs v-model="activeTab" @tab-change="fetchDocs">
      <el-tab-pane label="결재대기" name="pending" />
      <el-tab-pane label="진행중" name="inProgress" />
      <el-tab-pane label="완료" name="completed" />
      <el-tab-pane label="반려" name="rejected" />
    </el-tabs>
    <el-card style="margin-top: 8px;">
      <el-table :data="docs" v-loading="loading" style="width: 100%;" @row-click="openDoc">
        <el-table-column prop="title" label="제목" min-width="250" />
        <el-table-column prop="drafterName" label="기안자" width="100" />
        <el-table-column prop="formName" label="양식" width="120" />
        <el-table-column label="상태" width="100">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">{{ statusLabel(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="제출일" width="120">
          <template #default="{ row }">{{ formatDate(row.submittedAt) }}</template>
        </el-table-column>
      </el-table>
    </el-card>
    <el-dialog v-model="dialogVisible" title="결재 문서" width="700px">
      <div v-if="current">
        <h3>{{ current.title }}</h3>
        <el-descriptions :column="2" size="small" border>
          <el-descriptions-item label="기안자">{{ current.drafterName }}</el-descriptions-item>
          <el-descriptions-item label="상태">{{ statusLabel(current.status) }}</el-descriptions-item>
        </el-descriptions>
        <div style="margin-top: 16px;" v-html="formatFields(current.fields)" />
        <el-divider v-if="activeTab==='pending'" />
        <div v-if="activeTab==='pending'" style="display: flex; gap: 8px; align-items: center;">
          <span>의견:</span>
          <el-input v-model="comment" style="flex: 1;" placeholder="승인/반려 의견" />
          <el-button type="success" @click="approve">승인</el-button>
          <el-button type="danger" @click="reject">반려</el-button>
        </div>
      </div>
    </el-dialog>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
const activeTab = ref('pending'); const docs = ref<Record<string,unknown>[]>([]); const loading = ref(false)
const dialogVisible = ref(false); const current = ref<Record<string,unknown> | null>(null); const comment = ref('')
const endpointMap: Record<string,string> = { pending: '/approval/documents/pending', inProgress: '/approval/documents/in-progress', completed: '/approval/documents/completed', rejected: '/approval/documents/rejected' }
async function fetchDocs() {
  loading.value = true
  try { const r = await apiClient.get(endpointMap[activeTab.value], { params: { page: 1, pageSize: 50 } }); docs.value = r.data.data?.items || [] } catch { docs.value = [] } finally { loading.value = false }
}
async function openDoc(row: Record<string,unknown>) {
  try { const r = await apiClient.get(`/approval/documents/${row.id}`); current.value = r.data.data; dialogVisible.value = true } catch { ElMessage.error('문서 로딩 실패') }
}
async function approve() {
  try { await apiClient.post(`/approval/documents/${current.value!.id}/approve`, { comment: comment.value }); ElMessage.success('승인되었습니다'); dialogVisible.value = false; fetchDocs() } catch { ElMessage.error('처리 실패') }
}
async function reject() {
  try { await apiClient.post(`/approval/documents/${current.value!.id}/reject`, { comment: comment.value }); ElMessage.success('반려되었습니다'); dialogVisible.value = false; fetchDocs() } catch { ElMessage.error('처리 실패') }
}
function statusLabel(s: unknown) { return ({Pending:'대기',InProgress:'진행중',Approved:'승인',Rejected:'반려',Draft:'임시저장'} as Record<string,string>)[s as string] || String(s) }
function statusType(s: unknown) { return ({Pending:'warning',Approved:'success',Rejected:'danger',Draft:'info'}as Record<string,string>)[s as string] || '' }
function formatDate(v: unknown) { if (!v) return ''; return new Date(v as string).toLocaleDateString('ko-KR') }
function formatFields(fields: unknown) { if (!fields) return ''; try { return Object.entries(fields as Record<string,unknown>).map(([k,v]) => `<p><b>${k}:</b> ${v}</p>`).join('') } catch { return '' } }
onMounted(fetchDocs)
</script>
