<template>
  <div class="user-picker">
    <div class="selected-users" v-if="modelValue.length > 0" style="margin-bottom:8px;">
      <el-tag
        v-for="user in modelValue"
        :key="user.id"
        closable
        :disable-transitions="false"
        @close="removeUser(user.id)"
        style="margin:2px;"
      >
        {{ user.name }}
        <span v-if="user.departmentName" style="color:#aaa; font-size:11px;"> ({{ user.departmentName }})</span>
      </el-tag>
    </div>

    <el-select
      v-model="searchQuery"
      filterable
      remote
      :remote-method="searchUsers"
      :loading="searching"
      placeholder="사용자 검색..."
      value-key="id"
      clearable
      style="width:100%;"
      @change="onSelect"
    >
      <el-option
        v-for="u in searchResults"
        :key="u.id"
        :value="u"
        :label="u.name"
        :disabled="!multiple && modelValue.length >= 1"
      >
        <div style="display:flex; align-items:center; gap:8px;">
          <el-avatar :size="28" :src="u.profileImageUrl">{{ u.name.charAt(0) }}</el-avatar>
          <div>
            <div style="font-size:13px;">{{ u.name }}</div>
            <div style="font-size:11px; color:#999;">{{ u.departmentName }} · {{ u.positionName }}</div>
          </div>
        </div>
      </el-option>
    </el-select>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import apiClient from '@/shared/api/apiClient'

export interface PickedUser {
  id: string
  name: string
  departmentName?: string
  positionName?: string
  profileImageUrl?: string
}

const props = withDefaults(defineProps<{
  modelValue: PickedUser[]
  multiple?: boolean
  placeholder?: string
}>(), {
  multiple: true,
  placeholder: '사용자 검색...',
})

const emit = defineEmits<{
  'update:modelValue': [users: PickedUser[]]
}>()

const searchQuery = ref('')
const searchResults = ref<PickedUser[]>([])
const searching = ref(false)

async function searchUsers(query: string) {
  if (!query || query.length < 1) { searchResults.value = []; return }
  searching.value = true
  try {
    const r = await apiClient.get('/users', { params: { keyword: query, page: 1, pageSize: 20 } })
    searchResults.value = (r.data.data?.items ?? []).map((u: Record<string, unknown>) => ({
      id: u.id as string,
      name: u.name as string,
      departmentName: u.departmentName as string,
      positionName: u.positionName as string,
      profileImageUrl: u.profileImageUrl as string | undefined,
    }))
  } catch {
    searchResults.value = []
  } finally {
    searching.value = false
  }
}

function onSelect(user: PickedUser) {
  if (!user) return
  const already = props.modelValue.find(u => u.id === user.id)
  if (already) { searchQuery.value = ''; return }

  if (props.multiple) {
    emit('update:modelValue', [...props.modelValue, user])
  } else {
    emit('update:modelValue', [user])
  }
  searchQuery.value = ''
}

function removeUser(id: string) {
  emit('update:modelValue', props.modelValue.filter(u => u.id !== id))
}
</script>
