<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <el-page-header @back="$router.push('/board')" :title="boardName" />
      <el-button type="primary" @click="$router.push(`/board/${boardId}/write`)">글쓰기</el-button>
    </div>
    <el-card>
      <el-table :data="posts" v-loading="loading" style="width: 100%;" @row-click="(row: Record<string,unknown>) => gotoPost(row.id as string)">
        <el-table-column type="index" width="60" label="번호" />
        <el-table-column prop="title" label="제목" min-width="300">
          <template #default="{ row }">
            <el-tag v-if="row.isNotice" type="danger" size="small" style="margin-right: 4px;">공지</el-tag>
            {{ row.title }}
          </template>
        </el-table-column>
        <el-table-column prop="authorName" label="작성자" width="120" />
        <el-table-column prop="viewCount" label="조회" width="80" />
        <el-table-column label="작성일" width="120">
          <template #default="{ row }">{{ formatDate(row.createdAt) }}</template>
        </el-table-column>
      </el-table>
      <el-pagination v-model:current-page="page" :page-size="20" :total="total"
        layout="prev, pager, next" style="margin-top: 12px; text-align: center;"
        @current-change="fetchPosts" />
    </el-card>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import apiClient from '@/shared/api/apiClient'
const route = useRoute(); const router = useRouter()
const boardId = route.params.boardId as string
const boardName = ref(boardId); const posts = ref<Record<string,unknown>[]>([]); const loading = ref(false); const page = ref(1); const total = ref(0)
async function fetchPosts() {
  loading.value = true
  try { const r = await apiClient.get(`/boards/${boardId}/posts`, { params: { page: page.value, pageSize: 20 } }); posts.value = r.data.data?.items || []; total.value = r.data.data?.totalCount || 0 } catch { /* */ } finally { loading.value = false }
}
function gotoPost(id: string) { router.push(`/board/${boardId}/post/${id}`) }
function formatDate(v: string) { return new Date(v).toLocaleDateString('ko-KR') }
onMounted(fetchPosts)
</script>
