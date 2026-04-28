<template>
  <div>
    <h2 style="margin-bottom:16px;">자원 예약</h2>

    <el-row :gutter="16" style="margin-bottom:16px;">
      <!-- 자원 목록 (좌측) -->
      <el-col :span="6">
        <el-card>
          <template #header>
            <div style="display:flex; justify-content:space-between; align-items:center;">
              <span>자원 목록</span>
              <el-select v-model="typeFilter" placeholder="유형" size="small" style="width:100px;" clearable @change="loadResources">
                <el-option label="회의실" value="MeetingRoom" />
                <el-option label="차량" value="Vehicle" />
                <el-option label="장비" value="Equipment" />
              </el-select>
            </div>
          </template>
          <el-radio-group v-model="selectedResourceId" @change="loadReservations" style="width:100%;">
            <div v-for="r in resources" :key="r.id" style="margin-bottom:8px;">
              <el-radio :value="r.id" style="width:100%;">
                <div>
                  <div>{{ r.name }}</div>
                  <div style="font-size:11px; color:#999;">{{ resourceTypeLabel(r.type) }} · 수용: {{ r.capacity }}명</div>
                </div>
              </el-radio>
            </div>
            <div v-if="resources.length === 0" style="color:#ccc; padding:12px; text-align:center;">자원 없음</div>
          </el-radio-group>
        </el-card>
      </el-col>

      <!-- 예약 목록 (우측) -->
      <el-col :span="18">
        <el-card>
          <template #header>
            <div style="display:flex; justify-content:space-between; align-items:center;">
              <span>예약 현황 <span v-if="selectedResource">- {{ selectedResource?.name }}</span></span>
              <div>
                <el-date-picker
                  v-model="selectedDate"
                  type="date"
                  value-format="YYYY-MM-DD"
                  placeholder="날짜 선택"
                  size="small"
                  style="width:150px; margin-right:8px;"
                  @change="loadReservations"
                />
                <el-button type="primary" size="small" :disabled="!selectedResourceId" @click="showReserveDialog">예약하기</el-button>
              </div>
            </div>
          </template>

          <el-timeline v-if="reservations.length > 0">
            <el-timeline-item
              v-for="res in reservations"
              :key="res.id"
              :timestamp="`${res.startTime} - ${res.endTime}`"
              placement="top"
              :type="res.requesterId === myUserId ? 'primary' : 'info'"
            >
              <div style="display:flex; justify-content:space-between; align-items:center;">
                <div>
                  <strong>{{ res.title }}</strong>
                  <span style="font-size:12px; color:#999; margin-left:8px;">{{ res.requesterName }}</span>
                </div>
                <el-button
                  v-if="res.requesterId === myUserId"
                  size="small"
                  type="danger"
                  text
                  @click="cancelReservation(res.id)"
                >취소</el-button>
              </div>
              <div v-if="res.description" style="font-size:12px; color:#666; margin-top:4px;">{{ res.description }}</div>
            </el-timeline-item>
          </el-timeline>
          <div v-else style="text-align:center; color:#ccc; padding:24px;">
            {{ selectedResourceId ? '예약 내역이 없습니다.' : '자원을 선택하세요.' }}
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 예약 다이얼로그 -->
    <el-dialog v-model="reserveDialogVisible" title="자원 예약" width="480px">
      <el-form :model="reserveForm" label-width="90px">
        <el-form-item label="제목" required>
          <el-input v-model="reserveForm.title" placeholder="예약 제목" />
        </el-form-item>
        <el-form-item label="날짜" required>
          <el-date-picker
            v-model="reserveForm.date"
            type="date"
            value-format="YYYY-MM-DD"
            placeholder="날짜 선택"
            style="width:100%;"
          />
        </el-form-item>
        <el-form-item label="시작시간" required>
          <el-time-picker
            v-model="reserveForm.startTime"
            value-format="HH:mm"
            placeholder="시작"
            style="width:100%;"
          />
        </el-form-item>
        <el-form-item label="종료시간" required>
          <el-time-picker
            v-model="reserveForm.endTime"
            value-format="HH:mm"
            placeholder="종료"
            style="width:100%;"
          />
        </el-form-item>
        <el-form-item label="참석자">
          <UserPicker v-model="reserveForm.attendees" />
        </el-form-item>
        <el-form-item label="설명">
          <el-input v-model="reserveForm.description" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="reserveDialogVisible = false">취소</el-button>
        <el-button type="primary" :loading="reserving" @click="submitReservation">예약 확정</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
import UserPicker from '@/shared/components/UserPicker.vue'
import type { PickedUser } from '@/shared/components/UserPicker.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const myUserId = computed(() => authStore.user?.userId ?? '')

const typeFilter = ref('')
const resources = ref<Record<string, any>[]>([])
const selectedResourceId = ref<string>('')
const selectedDate = ref<string>(new Date().toISOString().substring(0, 10))
const reservations = ref<Record<string, any>[]>([])
const reserveDialogVisible = ref(false)
const reserving = ref(false)

const reserveForm = ref({
  title: '',
  date: selectedDate.value,
  startTime: '09:00',
  endTime: '10:00',
  description: '',
  attendees: [] as PickedUser[],
})

const selectedResource = computed(() => resources.value.find(r => r.id === selectedResourceId.value))

function resourceTypeLabel(type: string) {
  const map: Record<string, string> = { MeetingRoom: '회의실', Vehicle: '차량', Equipment: '장비' }
  return map[type] ?? type
}

async function loadResources() {
  try {
    const r = await apiClient.get('/calendar/resources', { params: { type: typeFilter.value || undefined } })
    resources.value = r.data.data ?? []
  } catch { /**/ }
}

async function loadReservations() {
  if (!selectedResourceId.value) { reservations.value = []; return }
  try {
    const r = await apiClient.get('/calendar/resources/' + selectedResourceId.value + '/reservations', {
      params: { date: selectedDate.value }
    })
    reservations.value = r.data.data ?? []
  } catch { /**/ }
}

function showReserveDialog() {
  reserveForm.value = {
    title: '',
    date: selectedDate.value,
    startTime: '09:00',
    endTime: '10:00',
    description: '',
    attendees: [],
  }
  reserveDialogVisible.value = true
}

async function submitReservation() {
  reserving.value = true
  try {
    const [startH, startM] = reserveForm.value.startTime.split(':')
    const [endH, endM] = reserveForm.value.endTime.split(':')
    const payload = {
      resourceId: selectedResourceId.value,
      title: reserveForm.value.title,
      startTime: `${reserveForm.value.date}T${startH}:${startM}:00`,
      endTime: `${reserveForm.value.date}T${endH}:${endM}:00`,
      description: reserveForm.value.description,
      attendeeUserIds: reserveForm.value.attendees.map(u => u.id),
    }
    await apiClient.post('/calendar/resources/reservations', payload)
    ElMessage.success('예약되었습니다.')
    reserveDialogVisible.value = false
    await loadReservations()
  } catch (e: unknown) {
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    ElMessage.error(msg || '예약에 실패했습니다.')
  } finally {
    reserving.value = false
  }
}

async function cancelReservation(id: string) {
  await ElMessageBox.confirm('예약을 취소하시겠습니까?', '확인', { type: 'warning' })
  try {
    await apiClient.delete('/calendar/resources/reservations/' + id)
    ElMessage.success('예약이 취소되었습니다.')
    await loadReservations()
  } catch { ElMessage.error('취소에 실패했습니다.') }
}

onMounted(() => {
  loadResources()
})
</script>
