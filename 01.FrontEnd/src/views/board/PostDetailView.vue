<template>
  <div>
    <el-page-header @back="$router.push(`/board/${boardId}`)" title="목록" style="margin-bottom: 16px;" />
    <el-card v-loading="loading">
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <h2 style="margin: 0;">{{ post.title }}</h2>
          <div>
            <el-button v-if="canEdit" @click="$router.push(`/board/${boardId}/write?postId=${postId}`)">수정</el-button>
            <el-button v-if="canEdit" type="danger" @click="deletePost">삭제</el-button>
          </div>
        </div>
        <el-descriptions :column="3" size="small" style="margin-top: 8px;">
          <el-descriptions-item label="작성자">{{ post.authorName }}</el-descriptions-item>
          <el-descriptions-item label="작성일">{{ formatDate(post.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="조회">{{ post.viewCount }}</el-descriptions-item>
        </el-descriptions>
      </template>
      <div v-html="post.content" style="min-height: 200px; line-height: 1.8;" />
    </el-card>
    <el-card style="margin-top: 16px;">
      <template #header><span>댓글 {{ comments.length }}</span></template>
      <div v-for="c in comments" :key="c.id" style="padding: 8px 0; border-bottom: 1px solid #f0f0f0;">
        <strong>{{ c.authorName }}</strong> <span style="color: #999; font-size: 12px;">{{ formatDate(c.createdAt) }}</span>
        <p style="margin: 4px 0;">{{ c.content }}</p>
      </div>
      <div style="margin-top: 12px; display: flex; gap: 8px;">
        <el-input v-model="newComment" placeholder="댓글을 입력하세요" />
        <el-button type="primary" @click="addComment">등록</el-button>
      </div>
    </el-card>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
import { useAuthStore } from '@/stores/auth'
const route = useRoute(); const router = useRouter(); const auth = useAuthStore()
const boardId = route.params.boardId as string; const postId = route.params.postId as string
const post = ref<Record<string,any>>({}); const comments = ref<Record<string,any>[]>([]); const loading = ref(false); const newComment = ref('')
const canEdit = ref(false)
async function fetch() {
  loading.value = true
  try {
    const r = await apiClient.get(`/boards/${boardId}/posts/${postId}`)
    post.value = r.data.data || {}
    canEdit.value = post.value.authorId === auth.user?.userId || auth.user?.systemRole === 'SystemAdmin'
    try { const cr = await apiClient.get(`/boards/${boardId}/posts/${postId}/comments`); comments.value = cr.data.data?.items || [] } catch { /**/ }
  } finally { loading.value = false }
}
async function addComment() {
  if (!newComment.value.trim()) return
  try { await apiClient.post(`/boards/${boardId}/posts/${postId}/comments`, { content: newComment.value }); newComment.value = ''; await fetch() } catch { ElMessage.error('댓글 등록 실패') }
}
async function deletePost() {
  await ElMessageBox.confirm('삭제하시겠습니까?', '확인', { type: 'warning' })
  try { await apiClient.delete(`/boards/${boardId}/posts/${postId}`); ElMessage.success('삭제되었습니다'); router.push(`/board/${boardId}`) } catch { ElMessage.error('삭제 실패') }
}
function formatDate(v: unknown) { if (!v) return ''; return new Date(v as string).toLocaleDateString('ko-KR') }
onMounted(fetch)
</script>
