<template>
  <div>
    <el-page-header @back="$router.go(-1)" title="취소" style="margin-bottom: 16px;" />
    <el-card>
      <el-form :model="form" label-width="80px">
        <el-form-item label="제목">
          <el-input v-model="form.title" placeholder="제목 입력" />
        </el-form-item>
        <el-form-item label="공지여부" v-if="isAdmin">
          <el-checkbox v-model="form.isNotice">공지글로 등록</el-checkbox>
        </el-form-item>
        <el-form-item label="내용">
          <el-input v-model="form.content" type="textarea" :rows="20" placeholder="내용 입력" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="submit" :loading="saving">등록</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
import { useAuthStore } from '@/stores/auth'
const route = useRoute(); const router = useRouter(); const auth = useAuthStore()
const boardId = route.params.boardId as string
const postId = route.query.postId as string | undefined
const isAdmin = auth.user?.role === 'Admin'
const form = ref({ title: '', content: '', isNotice: false })
const saving = ref(false)
onMounted(async () => {
  if (postId) {
    try { const r = await apiClient.get(`/boards/${boardId}/posts/${postId}`); const d = r.data.data; form.value.title = d.title; form.value.content = d.content; form.value.isNotice = d.isNotice } catch { /**/ }
  }
})
async function submit() {
  if (!form.value.title.trim()) { ElMessage.warning('제목을 입력하세요'); return }
  saving.value = true
  try {
    if (postId) { await apiClient.put(`/boards/${boardId}/posts/${postId}`, form.value) }
    else { await apiClient.post(`/boards/${boardId}/posts`, form.value) }
    ElMessage.success('저장되었습니다'); router.push(`/board/${boardId}`)
  } catch { ElMessage.error('저장 실패') } finally { saving.value = false }
}
</script>
