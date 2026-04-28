<template>
  <div>
    <h2 style="margin-bottom:16px;">관리자</h2>
    <el-tabs v-model="activeTab" type="border-card">

      <!-- 감사로그 -->
      <el-tab-pane label="감사로그" name="audit">
        <el-form inline @submit.prevent="loadAuditLogs">
          <el-form-item label="액션">
            <el-input v-model="auditQuery.action" placeholder="예: APPROVAL_APPROVE" clearable style="width:200px;" />
          </el-form-item>
          <el-form-item label="리소스">
            <el-input v-model="auditQuery.resourceType" placeholder="예: ApprovalDocument" clearable style="width:180px;" />
          </el-form-item>
          <el-form-item label="기간">
            <el-date-picker
              v-model="auditDateRange"
              type="daterange"
              value-format="YYYY-MM-DD"
              range-separator="~"
              start-placeholder="시작일"
              end-placeholder="종료일"
              style="width:220px;"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="loadAuditLogs">조회</el-button>
          </el-form-item>
        </el-form>
        <el-table :data="auditLogs" border stripe size="small" v-loading="auditLoading" style="margin-top:12px;">
          <el-table-column label="일시" prop="createdAt" width="160">
            <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
          </el-table-column>
          <el-table-column label="사용자" prop="actorName" width="100" />
          <el-table-column label="액션" prop="action" width="200" />
          <el-table-column label="리소스 유형" prop="resourceType" width="150" />
          <el-table-column label="리소스 ID" prop="resourceId" width="200" />
          <el-table-column label="IP" prop="ipAddress" width="130" />
        </el-table>
        <el-pagination
          v-if="auditTotal > 0"
          v-model:current-page="auditPage"
          :page-size="20"
          :total="auditTotal"
          layout="total, prev, pager, next"
          style="margin-top:12px;"
          @current-change="loadAuditLogs"
        />
      </el-tab-pane>

      <!-- 시스템 설정 -->
      <el-tab-pane label="시스템 설정" name="settings">
        <el-table :data="settings" border stripe size="small" v-loading="settingsLoading">
          <el-table-column label="카테고리" prop="category" width="120" />
          <el-table-column label="키" prop="key" width="220" />
          <el-table-column label="값">
            <template #default="{ row }">
              <el-input
                v-if="row.editing"
                v-model="row.newValue"
                size="small"
                style="width:80%; margin-right:8px;"
              />
              <span v-else>{{ row.isSecret ? '***' : row.value }}</span>
            </template>
          </el-table-column>
          <el-table-column label="설명" prop="description" />
          <el-table-column label="" width="140">
            <template #default="{ row }">
              <el-button v-if="!row.editing" size="small" @click="startEdit(row)">수정</el-button>
              <el-button v-if="row.editing" size="small" type="primary" :loading="savingSettings" @click="saveSetting(row)">저장</el-button>
              <el-button v-if="row.editing" size="small" @click="row.editing = false">취소</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>

      <!-- 코드 관리 -->
      <el-tab-pane label="코드 관리" name="codes">
        <div style="display:flex; gap:16px;">
          <div style="flex:1;">
            <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
              <el-input v-model="codeGroupFilter" placeholder="그룹코드 검색" clearable style="width:200px;" @input="loadCodes" />
              <el-button type="primary" size="small" @click="showCodeDialog(null)">코드 추가</el-button>
            </div>
            <el-table :data="codes" border stripe size="small" v-loading="codesLoading">
              <el-table-column label="그룹코드" prop="groupCode" width="130" />
              <el-table-column label="그룹명" prop="groupName" width="120" />
              <el-table-column label="코드" prop="code" width="100" />
              <el-table-column label="코드명" prop="name" />
              <el-table-column label="순서" prop="sortOrder" width="60" />
              <el-table-column label="활성" width="60">
                <template #default="{ row }">
                  <el-tag :type="row.isActive ? 'success' : 'info'" size="small">{{ row.isActive ? '활성' : '비활성' }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="" width="120">
                <template #default="{ row }">
                  <el-button size="small" @click="showCodeDialog(row)">수정</el-button>
                  <el-button size="small" type="danger" @click="deleteCode(row.id)">삭제</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </el-tab-pane>

      <!-- 메뉴 관리 -->
      <el-tab-pane label="메뉴 관리" name="menus">
        <div style="display:flex; justify-content:flex-end; margin-bottom:8px;">
          <el-button type="primary" size="small" @click="loadMenus">새로고침</el-button>
        </div>
        <el-tree
          :data="menuTree"
          :props="{ label: 'name', children: 'children' }"
          node-key="id"
          default-expand-all
          v-loading="menusLoading"
        >
          <template #default="{ node, data }">
            <div style="display:flex; align-items:center; width:100%; justify-content:space-between;">
              <span>
                <el-icon v-if="data.icon" style="margin-right:4px;"><component :is="data.icon" /></el-icon>
                {{ node.label }}
                <el-tag v-if="!data.isActive" size="small" type="info" style="margin-left:4px;">비활성</el-tag>
              </span>
              <span style="font-size:12px; color:#999;">{{ data.route }}</span>
            </div>
          </template>
        </el-tree>
      </el-tab-pane>

    </el-tabs>

    <!-- 코드 편집 다이얼로그 -->
    <el-dialog v-model="codeDialogVisible" :title="editingCode?.id ? '코드 수정' : '코드 추가'" width="500px">
      <el-form :model="codeForm" label-width="90px">
        <el-form-item label="그룹코드" required>
          <el-input v-model="codeForm.groupCode" placeholder="예: LEAVE_TYPE" />
        </el-form-item>
        <el-form-item label="그룹명" required>
          <el-input v-model="codeForm.groupName" placeholder="예: 휴가유형" />
        </el-form-item>
        <el-form-item label="코드" required>
          <el-input v-model="codeForm.code" placeholder="예: ANNUAL" />
        </el-form-item>
        <el-form-item label="코드명" required>
          <el-input v-model="codeForm.name" placeholder="예: 연차" />
        </el-form-item>
        <el-form-item label="순서">
          <el-input-number v-model="codeForm.sortOrder" :min="0" />
        </el-form-item>
        <el-form-item label="활성 여부">
          <el-switch v-model="codeForm.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="codeDialogVisible = false">취소</el-button>
        <el-button type="primary" :loading="savingCode" @click="saveCode">저장</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import apiClient from '@/shared/api/apiClient'

const activeTab = ref('audit')

// ─── 감사로그 ───
const auditLogs = ref<Record<string, unknown>[]>([])
const auditLoading = ref(false)
const auditPage = ref(1)
const auditTotal = ref(0)
const auditDateRange = ref<string[]>([])
const auditQuery = ref({ action: '', resourceType: '' })

async function loadAuditLogs() {
  auditLoading.value = true
  try {
    const params: Record<string, unknown> = {
      page: auditPage.value, pageSize: 20,
      action: auditQuery.value.action || undefined,
      resourceType: auditQuery.value.resourceType || undefined,
      from: auditDateRange.value?.[0],
      to: auditDateRange.value?.[1],
    }
    const r = await apiClient.get('/admin/audit-logs', { params })
    auditLogs.value = r.data.data?.items ?? []
    auditTotal.value = r.data.data?.totalCount ?? 0
  } catch { /**/ } finally { auditLoading.value = false }
}

// ─── 시스템 설정 ───
const settings = ref<Record<string, unknown>[]>([])
const settingsLoading = ref(false)
const savingSettings = ref(false)

async function loadSettings() {
  settingsLoading.value = true
  try {
    const r = await apiClient.get('/admin/settings')
    settings.value = (r.data.data ?? []).map((s: Record<string, unknown>) => ({ ...s, editing: false, newValue: s.value }))
  } catch { /**/ } finally { settingsLoading.value = false }
}

function startEdit(row: Record<string, unknown>) {
  row.editing = true
  row.newValue = row.value
}

async function saveSetting(row: Record<string, unknown>) {
  savingSettings.value = true
  try {
    await apiClient.put(`/admin/settings/${row.key}`, { value: row.newValue })
    row.value = row.newValue
    row.editing = false
    ElMessage.success('저장되었습니다.')
  } catch { ElMessage.error('저장에 실패했습니다.') } finally { savingSettings.value = false }
}

// ─── 코드 관리 ───
const codes = ref<Record<string, unknown>[]>([])
const codesLoading = ref(false)
const codeGroupFilter = ref('')
const codeDialogVisible = ref(false)
const editingCode = ref<Record<string, unknown> | null>(null)
const savingCode = ref(false)
const codeForm = ref({ groupCode: '', groupName: '', code: '', name: '', sortOrder: 0, isActive: true })

async function loadCodes() {
  codesLoading.value = true
  try {
    const r = await apiClient.get('/admin/codes', {
      params: { groupCode: codeGroupFilter.value || undefined, page: 1, pageSize: 100 }
    })
    codes.value = r.data.data?.items ?? []
  } catch { /**/ } finally { codesLoading.value = false }
}

function showCodeDialog(code: Record<string, unknown> | null) {
  editingCode.value = code
  if (code) {
    codeForm.value = { groupCode: code.groupCode as string, groupName: code.groupName as string, code: code.code as string, name: code.name as string, sortOrder: code.sortOrder as number, isActive: code.isActive as boolean }
  } else {
    codeForm.value = { groupCode: '', groupName: '', code: '', name: '', sortOrder: 0, isActive: true }
  }
  codeDialogVisible.value = true
}

async function saveCode() {
  savingCode.value = true
  try {
    await apiClient.post('/admin/codes', codeForm.value)
    ElMessage.success('저장되었습니다.')
    codeDialogVisible.value = false
    await loadCodes()
  } catch { ElMessage.error('저장에 실패했습니다.') } finally { savingCode.value = false }
}

async function deleteCode(id: string) {
  await ElMessageBox.confirm('코드를 삭제하시겠습니까?', '확인', { type: 'warning' })
  try {
    await apiClient.delete(`/admin/codes/${id}`)
    ElMessage.success('삭제되었습니다.')
    await loadCodes()
  } catch { ElMessage.error('삭제에 실패했습니다.') }
}

// ─── 메뉴 관리 ───
const menuTree = ref<Record<string, unknown>[]>([])
const menusLoading = ref(false)

async function loadMenus() {
  menusLoading.value = true
  try {
    const r = await apiClient.get('/admin/menus')
    menuTree.value = r.data.data ?? []
  } catch { /**/ } finally { menusLoading.value = false }
}

function formatDate(d: string) {
  return new Date(d).toLocaleString('ko-KR', { timeZone: 'Asia/Seoul' })
}

onMounted(() => {
  loadAuditLogs()
  loadSettings()
  loadCodes()
  loadMenus()
})
</script>
