<template>
  <div>
    <el-row :gutter="16" style="margin-bottom: 16px;">
      <el-col :span="6">
        <el-card shadow="hover" class="dashboard-card" @click="$router.push('/approval')">
          <div class="stat-card">
            <div class="stat-icon approval"><el-icon size="32"><Finished /></el-icon></div>
            <div>
              <div class="stat-value">{{ dashboard?.pendingApprovalCount ?? 0 }}</div>
              <div class="stat-label">결재 대기</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="dashboard-card">
          <div class="stat-card">
            <div class="stat-icon attendance"><el-icon size="32"><Timer /></el-icon></div>
            <div>
              <div class="stat-value">{{ dashboard?.todayAttendance?.status ?? '-' }}</div>
              <div class="stat-label">오늘 근태</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="dashboard-card">
          <div class="stat-card">
            <div class="stat-icon leave"><el-icon size="32"><Calendar /></el-icon></div>
            <div>
              <div class="stat-value">{{ dashboard?.leaveBalance?.remaining ?? 0 }}일</div>
              <div class="stat-label">잔여 연차</div>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover" class="dashboard-card" @click="$router.push('/notifications')">
          <div class="stat-card">
            <div class="stat-icon notify"><el-icon size="32"><Bell /></el-icon></div>
            <div>
              <div class="stat-value">{{ dashboard?.unreadNotificationCount ?? 0 }}</div>
              <div class="stat-label">미읽은 알림</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-card>
          <template #header>
            <div style="display: flex; justify-content: space-between;">
              <span>📢 최근 공지사항</span>
              <el-button text size="small" @click="$router.push('/board')">더보기</el-button>
            </div>
          </template>
          <el-timeline v-if="dashboard?.recentNotices?.length">
            <el-timeline-item v-for="n in dashboard.recentNotices" :key="n.id"
              :timestamp="formatDate(n.createdAt)" placement="top">
              <div style="cursor: pointer" @click="gotoPost(n.boardId, n.id)">{{ n.title }}</div>
            </el-timeline-item>
          </el-timeline>
          <el-empty v-else description="공지사항이 없습니다." :image-size="60" />
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card>
          <template #header>
            <div style="display: flex; justify-content: space-between;">
              <span>📅 오늘의 일정</span>
              <el-button text size="small" @click="$router.push('/calendar')">더보기</el-button>
            </div>
          </template>
          <div v-if="dashboard?.todayEvents?.length">
            <div v-for="ev in dashboard.todayEvents" :key="ev.id" class="event-item">
              <el-tag size="small">{{ formatTime(ev.startAt) }}</el-tag>
              <span>{{ ev.title }}</span>
            </div>
          </div>
          <el-empty v-else description="오늘 일정이 없습니다." :image-size="60" />
        </el-card>
      </el-col>
    </el-row>

    <!-- Quick actions -->
    <el-card style="margin-top: 16px;">
      <template #header><span>⚡ 빠른 실행</span></template>
      <div style="display: flex; gap: 12px; flex-wrap: wrap;">
        <el-button @click="checkIn" :disabled="checkedIn" type="primary" plain>
          {{ checkedIn ? '✅ 출근 완료' : '출근 체크인' }}
        </el-button>
        <el-button @click="checkOut" type="warning" plain>퇴근 체크아웃</el-button>
        <el-button @click="$router.push('/approval/draft')" type="success" plain>결재 기안</el-button>
        <el-button @click="$router.push('/board/NOTICE/write')" plain>공지 작성</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '@/shared/api/apiClient'
import { ElMessage } from 'element-plus'

const router = useRouter()
const dashboard = ref<Record<string, any> | null>(null)
const checkedIn = ref(false)

async function fetchDashboard() {
  try {
    const res = await apiClient.get('/portal/dashboard')
    dashboard.value = res.data.data
    checkedIn.value = dashboard.value?.todayAttendance != null &&
      (dashboard.value?.todayAttendance as Record<string,unknown>)?.checkInAt != null
  } catch { /* ignore */ }
}

async function checkIn() {
  try {
    await apiClient.post('/attendance/check-in', { workType: 'Office' })
    ElMessage.success('출근 처리되었습니다.')
    checkedIn.value = true
    fetchDashboard()
  } catch (e: unknown) {
    ElMessage.error((e as { response?: { data?: { message?: string } } })?.response?.data?.message || '오류가 발생했습니다.')
  }
}

async function checkOut() {
  try {
    await apiClient.post('/attendance/check-out', {})
    ElMessage.success('퇴근 처리되었습니다.')
  } catch (e: unknown) {
    ElMessage.error((e as { response?: { data?: { message?: string } } })?.response?.data?.message || '오류가 발생했습니다.')
  }
}

function gotoPost(boardId: string, postId: string) {
  router.push(`/board/${boardId}/post/${postId}`)
}

function formatDate(v: string) {
  return new Date(v).toLocaleDateString('ko-KR')
}

function formatTime(v: string) {
  return new Date(v).toLocaleTimeString('ko-KR', { hour: '2-digit', minute: '2-digit' })
}

onMounted(fetchDashboard)
</script>

<style scoped>
.dashboard-card { cursor: pointer; transition: transform 0.2s; }
.dashboard-card:hover { transform: translateY(-2px); }
.stat-card { display: flex; align-items: center; gap: 16px; }
.stat-icon { width: 60px; height: 60px; border-radius: 12px; display: flex; align-items: center; justify-content: center; }
.stat-icon.approval { background: #fff2e8; color: #fa541c; }
.stat-icon.attendance { background: #e6f7ff; color: #1890ff; }
.stat-icon.leave { background: #f6ffed; color: #52c41a; }
.stat-icon.notify { background: #f9f0ff; color: #722ed1; }
.stat-value { font-size: 28px; font-weight: bold; line-height: 1; }
.stat-label { color: #999; margin-top: 4px; }
.event-item { display: flex; align-items: center; gap: 8px; padding: 6px 0; border-bottom: 1px solid #f0f0f0; }
.event-item:last-child { border-bottom: none; }
</style>
