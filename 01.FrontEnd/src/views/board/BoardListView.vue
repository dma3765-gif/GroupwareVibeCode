<template>
  <div>
    <el-page-header @back="$router.push('/')" title="게시판 목록" />
    <el-row :gutter="16" style="margin-top: 16px;">
      <el-col :span="6" v-for="board in boards" :key="board.id">
        <el-card shadow="hover" style="cursor: pointer; margin-bottom: 16px;" @click="$router.push(`/board/${board.code}`)">
          <div style="text-align: center; padding: 12px 0;">
            <el-icon size="40" color="#1890ff"><Memo /></el-icon>
            <div style="font-size: 18px; font-weight: bold; margin-top: 8px;">{{ board.name }}</div>
            <div style="color: #999; font-size: 12px;">{{ board.description }}</div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '@/shared/api/apiClient'
const boards = ref<Record<string, unknown>[]>([])
onMounted(async () => {
  try { const r = await apiClient.get('/boards'); boards.value = r.data.data || [] } catch { /* */ }
})
</script>
