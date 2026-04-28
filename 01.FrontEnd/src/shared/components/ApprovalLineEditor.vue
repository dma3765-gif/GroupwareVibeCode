<template>
  <div class="approval-line-editor">
    <div style="margin-bottom:12px; display:flex; gap:8px; flex-wrap:wrap;">
      <el-select v-model="addRole" placeholder="역할 선택" style="width:120px;" size="small">
        <el-option label="결재" value="Approval" />
        <el-option label="합의" value="Agreement" />
        <el-option label="협의" value="Consultation" />
      </el-select>

      <el-select
        v-model="selectedUserId"
        filterable
        remote
        :remote-method="searchUsers"
        :loading="searching"
        placeholder="결재자 검색..."
        clearable
        size="small"
        style="width:260px;"
        @change="addApprover"
      >
        <el-option
          v-for="u in searchResults"
          :key="u.id"
          :value="u.id"
          :label="`${u.name} (${u.departmentName})`"
        />
      </el-select>

      <el-checkbox
        v-if="addRole === 'Approval'"
        v-model="addFinalApproval"
        size="small"
      >전결 허용</el-checkbox>
    </div>

    <!-- 결재선 목록 -->
    <div class="approval-line-list">
      <div
        v-for="(item, index) in modelValue"
        :key="index"
        class="approval-line-item"
      >
        <span class="seq">{{ index + 1 }}</span>
        <el-tag size="small" :type="roleTagType(item.role)" style="min-width:48px; text-align:center;">
          {{ roleLabel(item.role) }}
        </el-tag>
        <div class="user-info">
          <span class="name">{{ item.name || item.userId }}</span>
          <span class="dept-pos" v-if="item.departmentName">{{ item.departmentName }} · {{ item.positionName }}</span>
        </div>
        <el-tag v-if="item.isFinalApprovalAllowed" size="small" type="warning">전결</el-tag>
        <el-button-group size="small">
          <el-button :disabled="index === 0" @click="moveUp(index)" text>↑</el-button>
          <el-button :disabled="index === modelValue.length - 1" @click="moveDown(index)" text>↓</el-button>
          <el-button type="danger" text @click="remove(index)">×</el-button>
        </el-button-group>
      </div>

      <div v-if="modelValue.length === 0" style="color:#ccc; text-align:center; padding:16px;">
        결재선을 추가해주세요
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import apiClient from '@/shared/api/apiClient'

export interface ApprovalLineItem {
  seq: number
  role: string
  userId: string
  name?: string
  departmentName?: string
  positionName?: string
  isFinalApprovalAllowed?: boolean
}

interface UserSearchResult {
  id: string
  name: string
  departmentName: string
  positionName: string
}

const props = defineProps<{
  modelValue: ApprovalLineItem[]
}>()

const emit = defineEmits<{
  'update:modelValue': [items: ApprovalLineItem[]]
}>()

const addRole = ref('Approval')
const addFinalApproval = ref(false)
const selectedUserId = ref('')
const searching = ref(false)
const searchResults = ref<UserSearchResult[]>([])

async function searchUsers(query: string) {
  if (!query) { searchResults.value = []; return }
  searching.value = true
  try {
    const r = await apiClient.get('/users', { params: { keyword: query, page: 1, pageSize: 20 } })
    searchResults.value = (r.data.data?.items ?? []).map((u: Record<string, unknown>) => ({
      id: u.id as string,
      name: u.name as string,
      departmentName: u.departmentName as string,
      positionName: u.positionName as string,
    }))
  } catch {
    searchResults.value = []
  } finally {
    searching.value = false
  }
}

function addApprover(userId: string) {
  if (!userId) return
  const user = searchResults.value.find(u => u.id === userId)
  if (!user) return

  const newItem: ApprovalLineItem = {
    seq: props.modelValue.length + 1,
    role: addRole.value,
    userId: user.id,
    name: user.name,
    departmentName: user.departmentName,
    positionName: user.positionName,
    isFinalApprovalAllowed: addRole.value === 'Approval' && addFinalApproval.value,
  }

  const updated = [...props.modelValue, newItem].map((item, idx) => ({ ...item, seq: idx + 1 }))
  emit('update:modelValue', updated)
  selectedUserId.value = ''
}

function remove(index: number) {
  const updated = props.modelValue.filter((_, i) => i !== index).map((item, idx) => ({ ...item, seq: idx + 1 }))
  emit('update:modelValue', updated)
}

function moveUp(index: number) {
  if (index === 0) return
  const arr = [...props.modelValue]
  ;[arr[index - 1], arr[index]] = [arr[index], arr[index - 1]]
  emit('update:modelValue', arr.map((item, idx) => ({ ...item, seq: idx + 1 })))
}

function moveDown(index: number) {
  if (index === props.modelValue.length - 1) return
  const arr = [...props.modelValue]
  ;[arr[index], arr[index + 1]] = [arr[index + 1], arr[index]]
  emit('update:modelValue', arr.map((item, idx) => ({ ...item, seq: idx + 1 })))
}

function roleLabel(role: string) {
  const map: Record<string, string> = { Approval: '결재', Agreement: '합의', Consultation: '협의' }
  return map[role] ?? role
}

function roleTagType(role: string) {
  if (role === 'Approval') return ''
  if (role === 'Agreement') return 'success'
  return 'info'
}
</script>

<style scoped>
.approval-line-list { display: flex; flex-direction: column; gap: 6px; }
.approval-line-item {
  display: flex; align-items: center; gap: 8px;
  padding: 6px 10px; border: 1px solid #eee; border-radius: 4px;
}
.seq { width: 20px; text-align: center; color: #999; font-size: 12px; }
.user-info { flex: 1; }
.name { font-size: 13px; font-weight: 500; }
.dept-pos { display: block; font-size: 11px; color: #999; }
</style>
