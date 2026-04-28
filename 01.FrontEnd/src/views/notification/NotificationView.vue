<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <h2 style="margin: 0;">알림</h2>
      <el-button @click="markAllRead" :disabled="!unreadCount">전체 읽음</el-button>
    </div>
    <el-card>
      <div style="margin-bottom: 12px;">
        <el-radio-group v-model="filter" @change="fetchNotifications">
          <el-radio-button value="all">전체</el-radio-button>
          <el-radio-button value="unread">미읽음 <el-badge v-if="unreadCount" :value="unreadCount" /></el-radio-button>
        </el-radio-group>
      </div>
      <div v-loading="loading">
        <div v-for="n in notifications" :key="n.id" :style="`padding: 12px; margin-bottom: 8px; border-radius: 6px; cursor: pointer; background: ${n.isRead ? '#fafafa' : '#ecf5ff'}; border: 1px solid ${n.isRead ? '#e8e8e8' : '#b3d8ff'}`" @click="markRead(n)">
          <div style="display: flex; justify-content: space-between; align-items: flex-start;">
            <div>
              <el-tag :type="notifType(n.notificationType)" size="small" style="margin-right: 8px;">{{ notifLabel(n.notificationType) }}</el-tag>
              <span :style="n.isRead ? '' : 'font-weight: bold;'">{{ n.title }}</span>
            </div>
            <span style="color: #999; font-size: 12px;">{{ formatDate(n.createdAt) }}</span>
          </div>
          <p style="margin: 4px 0 0 0; color: #666; font-size: 13px;">{{ n.body }}</p>
        </div>
        <el-empty v-if="!notifications.length && !loading" description="알림이 없습니다" />
      </div>
      <el-pagination v-model:current-page="page" :page-size="20" :total="total" layout="prev, pager, next" style="margin-top: 12px; text-align: center;" @current-change="fetchNotifications" />
    </el-card>
  </div>
</template>
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
const loading = ref(false); const notifications = ref<Record<string,any>[]>([]); const total = ref(0); const page = ref(1); const filter = ref('all')
const unreadCount = computed(() => notifications.value.filter(n => !n.isRead).length)
async function fetchNotifications() {
  loading.value = true
  try {
    const params: Record<string,unknown> = { page: page.value, pageSize: 20 }
    if (filter.value === 'unread') params.unreadOnly = true
    const r = await apiClient.get('/notifications', { params }); notifications.value = r.data.data?.items || []; total.value = r.data.data?.totalCount || 0
  } catch { /**/ } finally { loading.value = false }
}
async function markRead(n: Record<string,unknown>) {
  if (n.isRead) return
  try { await apiClient.post(`/notifications/${n.id}/read`); n.isRead = true } catch { /**/ }
}
async function markAllRead() {
  try { await apiClient.post('/notifications/read-all'); notifications.value.forEach(n => { n.isRead = true }); ElMessage.success('전체 읽음 처리되었습니다') } catch { ElMessage.error('처리 실패') }
}
function notifLabel(t: unknown) { return ({ApprovalRequested:'결재요청',ApprovalApproved:'승인',ApprovalRejected:'반려',PostCreated:'새글',MentionInPost:'멘션',SystemNotice:'공지',LeaveApproved:'휴가승인',LeaveRejected:'휴가반려'} as Record<string,string>)[t as string] || String(t) }
function notifType(t: unknown) { return ({ApprovalApproved:'success',ApprovalRejected:'danger',ApprovalRequested:'warning'} as Record<string,string>)[t as string] || 'info' }
function formatDate(v: unknown) { if (!v) return ''; const d = new Date(v as string); return d.toLocaleDateString('ko-KR') + ' ' + d.toLocaleTimeString('ko-KR', { hour:'2-digit', minute:'2-digit' }) }
onMounted(fetchNotifications)
</script>
