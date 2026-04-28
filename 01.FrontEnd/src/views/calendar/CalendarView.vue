<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <h2 style="margin: 0;">캘린더</h2>
      <el-button type="primary" @click="showCreate = true">일정 추가</el-button>
    </div>
    <el-row :gutter="16">
      <el-col :span="17">
        <el-calendar v-model="currentDate" @input="onMonthChange">
          <template #date-cell="{ data }">
            <div style="height: 100%; padding: 2px;">
              <span :class="data.isSelected ? 'selected-day' : ''">{{ data.day.split('-').slice(2).join('') }}</span>
              <div v-for="ev in eventsOnDay(data.day)" :key="ev.id" :style="`background:${ev.color||'#409eff'};color:#fff;border-radius:3px;font-size:11px;padding:1px 4px;margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;cursor:pointer;`" @click.stop="viewEvent(ev)">{{ ev.title }}</div>
            </div>
          </template>
        </el-calendar>
      </el-col>
      <el-col :span="7">
        <el-card>
          <template #header><b>이번달 일정</b></template>
          <div v-for="ev in events" :key="ev.id" style="padding: 6px 0; border-bottom: 1px solid #f0f0f0; cursor: pointer;" @click="viewEvent(ev)">
            <el-tag :color="ev.color || '#409eff'" style="color:#fff;border:none;" size="small">{{ formatDate(ev.startDateTime) }}</el-tag>
            <span style="margin-left: 6px; font-size: 13px;">{{ ev.title }}</span>
          </div>
          <el-empty v-if="!events.length" description="일정 없음" :image-size="60" />
        </el-card>
      </el-col>
    </el-row>

    <!-- Create/Edit Dialog -->
    <el-dialog v-model="showCreate" :title="editingEvent ? '일정 수정' : '일정 추가'" width="500px">
      <el-form :model="evForm" label-width="80px">
        <el-form-item label="제목"><el-input v-model="evForm.title" /></el-form-item>
        <el-form-item label="캘린더">
          <el-select v-model="evForm.calendarId" style="width: 100%;">
            <el-option v-for="c in calendars" :key="c.id" :label="c.name" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="시작">
          <el-date-picker v-model="evForm.startDateTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="종료">
          <el-date-picker v-model="evForm.endDateTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="색상"><el-color-picker v-model="evForm.color" /></el-form-item>
        <el-form-item label="메모"><el-input v-model="evForm.description" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCreate = false">취소</el-button>
        <el-button v-if="editingEvent" type="danger" @click="deleteEvent">삭제</el-button>
        <el-button type="primary" @click="saveEvent" :loading="saving">저장</el-button>
      </template>
    </el-dialog>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
const currentDate = ref(new Date()); const events = ref<Record<string,unknown>[]>([]); const calendars = ref<Record<string,unknown>[]>([])
const showCreate = ref(false); const editingEvent = ref<Record<string,unknown>|null>(null); const saving = ref(false)
const evForm = ref({ title:'', calendarId:'', startDateTime:'', endDateTime:'', color:'#409eff', description:'' })
function formatDate(v: unknown) { if (!v) return ''; return new Date(v as string).toLocaleDateString('ko-KR') }
function eventsOnDay(day: string) { return events.value.filter(e => (e.startDateTime as string)?.startsWith(day)) }
function viewEvent(ev: Record<string,unknown>) {
  editingEvent.value = ev
  evForm.value = { title: ev.title as string, calendarId: ev.calendarId as string, startDateTime: ev.startDateTime as string, endDateTime: ev.endDateTime as string, color: (ev.color as string)||'#409eff', description: (ev.description as string)||'' }
  showCreate.value = true
}
async function fetchEvents() {
  const d = currentDate.value; const start = new Date(d.getFullYear(), d.getMonth(), 1).toISOString(); const end = new Date(d.getFullYear(), d.getMonth()+1, 0).toISOString()
  try { const r = await apiClient.get('/calendars/events', { params: { start, end } }); events.value = r.data.data?.items || r.data.data || [] } catch { /**/ }
}
async function fetchCalendars() { try { const r = await apiClient.get('/calendars'); calendars.value = r.data.data?.items || r.data.data || [] } catch { /**/ } }
async function saveEvent() {
  saving.value = true
  try {
    if (editingEvent.value) { await apiClient.put(`/calendars/${evForm.value.calendarId}/events/${editingEvent.value.id}`, evForm.value) }
    else { await apiClient.post(`/calendars/${evForm.value.calendarId}/events`, evForm.value) }
    ElMessage.success('저장되었습니다'); showCreate.value = false; editingEvent.value = null; fetchEvents()
  } catch { ElMessage.error('저장 실패') } finally { saving.value = false }
}
async function deleteEvent() {
  try { await apiClient.delete(`/calendars/${evForm.value.calendarId}/events/${editingEvent.value!.id}`); ElMessage.success('삭제되었습니다'); showCreate.value = false; editingEvent.value = null; fetchEvents() } catch { ElMessage.error('삭제 실패') }
}
function onMonthChange() { fetchEvents() }
onMounted(() => { fetchCalendars(); fetchEvents() })
</script>
