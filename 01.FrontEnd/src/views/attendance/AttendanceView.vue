<template>
  <div>
    <h2 style="margin-bottom: 16px;">근태관리</h2>
    <el-row :gutter="16" style="margin-bottom: 16px;">
      <el-col :span="12">
        <el-card>
          <template #header><b>오늘 근태</b></template>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="날짜">{{ today }}</el-descriptions-item>
            <el-descriptions-item label="상태"><el-tag :type="todayStatus.type">{{ todayStatus.label }}</el-tag></el-descriptions-item>
            <el-descriptions-item label="출근">{{ todayRecord.checkInTime || '-' }}</el-descriptions-item>
            <el-descriptions-item label="퇴근">{{ todayRecord.checkOutTime || '-' }}</el-descriptions-item>
          </el-descriptions>
          <div style="margin-top: 12px; display: flex; gap: 8px;">
            <el-button type="primary" @click="checkIn" :disabled="!!todayRecord.checkInTime">출근체크인</el-button>
            <el-button type="warning" @click="checkOut" :disabled="!todayRecord.checkInTime || !!todayRecord.checkOutTime">퇴근체크아웃</el-button>
          </div>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card>
          <template #header><b>연차 현황</b></template>
          <div v-for="b in leaveBalances" :key="b.leaveType" style="display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid #f0f0f0;">
            <span>{{ b.leaveTypeName }}</span>
            <span>잔여 <b>{{ b.remainingDays }}</b>일 / {{ b.totalDays }}일</span>
          </div>
          <el-empty v-if="!leaveBalances.length" description="연차 정보 없음" :image-size="60" />
        </el-card>
      </el-col>
    </el-row>

    <el-card style="margin-bottom: 16px;">
      <template #header>
        <div style="display: flex; justify-content: space-between;">
          <b>이번달 근태내역</b>
          <div>
            <el-date-picker v-model="selectedMonth" type="month" value-format="YYYY-MM" style="width: 150px;" @change="fetchMonthly" />
          </div>
        </div>
      </template>
      <el-table :data="records" v-loading="loading">
        <el-table-column label="날짜" prop="date" width="120" />
        <el-table-column label="출근" prop="checkInTime" width="120" />
        <el-table-column label="퇴근" prop="checkOutTime" width="120" />
        <el-table-column label="상태" width="100">
          <template #default="{ row }">
            <el-tag :type="recordStatusType(row.status)">{{ row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="근무시간" prop="workingHours" width="100" />
      </el-table>
    </el-card>

    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between;">
          <b>휴가 신청</b>
          <el-button type="primary" size="small" @click="showLeaveDialog = true">신청</el-button>
        </div>
      </template>
      <el-table :data="leaveRequests" v-loading="loadingLeave">
        <el-table-column prop="leaveTypeName" label="휴가종류" width="120" />
        <el-table-column prop="startDate" label="시작일" width="120" />
        <el-table-column prop="endDate" label="종료일" width="120" />
        <el-table-column prop="daysCount" label="일수" width="80" />
        <el-table-column label="상태" width="100">
          <template #default="{ row }"><el-tag :type="leaveStatusType(row.status)">{{ row.status }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="reason" label="사유" />
      </el-table>
    </el-card>

    <el-dialog v-model="showLeaveDialog" title="휴가 신청" width="500px">
      <el-form :model="leaveForm" label-width="100px">
        <el-form-item label="휴가종류">
          <el-select v-model="leaveForm.leaveType" style="width: 100%;">
            <el-option label="연차" value="Annual" /><el-option label="반차(오전)" value="HalfAM" /><el-option label="반차(오후)" value="HalfPM" /><el-option label="병가" value="Sick" />
          </el-select>
        </el-form-item>
        <el-form-item label="기간">
          <el-date-picker v-model="leaveForm.dateRange" type="daterange" value-format="YYYY-MM-DD" start-placeholder="시작일" end-placeholder="종료일" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="사유"><el-input v-model="leaveForm.reason" type="textarea" :rows="3"/></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showLeaveDialog = false">취소</el-button>
        <el-button type="primary" @click="applyLeave" :loading="savingLeave">신청</el-button>
      </template>
    </el-dialog>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
const loading = ref(false); const loadingLeave = ref(false); const today = new Date().toLocaleDateString('ko-KR')
const selectedMonth = ref(new Date().toISOString().slice(0,7))
const todayRecord = ref<Record<string,unknown>>({}); const records = ref<Record<string,unknown>[]>([]); const leaveBalances = ref<Record<string,unknown>[]>([]); const leaveRequests = ref<Record<string,unknown>[]>([])
const showLeaveDialog = ref(false); const savingLeave = ref(false)
const leaveForm = ref({ leaveType: 'Annual', dateRange: [] as string[], reason: '' })
const todayStatus = computed(() => {
  const r = todayRecord.value
  if (r.checkOutTime) return { type: 'success', label: '퇴근' }
  if (r.checkInTime) return { type: 'primary', label: '근무중' }
  return { type: 'info', label: '미체크인' }
})
async function fetchToday() {
  try { const r = await apiClient.get('/attendance/today'); todayRecord.value = r.data.data || {} } catch { /**/ }
}
async function fetchMonthly() {
  loading.value = true
  try { const [year, month] = selectedMonth.value.split('-'); const r = await apiClient.get('/attendance/my', { params: { year, month } }); records.value = r.data.data?.items || r.data.data || [] } catch { records.value = [] } finally { loading.value = false }
}
async function fetchLeaveBalances() { try { const r = await apiClient.get('/attendance/leave-balance'); leaveBalances.value = r.data.data || [] } catch { /**/ } }
async function fetchLeaveRequests() {
  loadingLeave.value = true
  try { const r = await apiClient.get('/attendance/leave-requests', { params: { page:1, pageSize:50 } }); leaveRequests.value = r.data.data?.items || [] } catch { /**/ } finally { loadingLeave.value = false }
}
async function checkIn() { try { await apiClient.post('/attendance/check-in'); ElMessage.success('출근 체크인 완료'); fetchToday() } catch { ElMessage.error('체크인 실패') } }
async function checkOut() { try { await apiClient.post('/attendance/check-out'); ElMessage.success('퇴근 체크아웃 완료'); fetchToday() } catch { ElMessage.error('체크아웃 실패') } }
async function applyLeave() {
  savingLeave.value = true
  try {
    await apiClient.post('/attendance/leave-requests', { leaveType: leaveForm.value.leaveType, startDate: leaveForm.value.dateRange[0], endDate: leaveForm.value.dateRange[1], reason: leaveForm.value.reason })
    ElMessage.success('신청되었습니다'); showLeaveDialog.value = false; fetchLeaveRequests()
  } catch { ElMessage.error('신청 실패') } finally { savingLeave.value = false }
}
function recordStatusType(s: unknown) { return ({Normal:'success',Late:'warning',Absent:'danger',HalfDay:'info'} as Record<string,string>)[s as string] || '' }
function leaveStatusType(s: unknown) { return ({Pending:'warning',Approved:'success',Rejected:'danger'} as Record<string,string>)[s as string] || '' }
onMounted(() => { fetchToday(); fetchMonthly(); fetchLeaveBalances(); fetchLeaveRequests() })
</script>
