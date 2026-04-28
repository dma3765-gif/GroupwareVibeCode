<template>
  <div class="portal-settings">
    <el-card>
      <template #header>
        <div style="display: flex; align-items: center; justify-content: space-between;">
          <span style="font-size: 18px; font-weight: 600;">포털 웹파트 설정</span>
          <div style="display: flex; gap: 8px;">
            <el-button @click="$router.back()">취소</el-button>
            <el-button type="primary" :loading="saving" @click="saveLayout">저장</el-button>
          </div>
        </div>
      </template>

      <el-row :gutter="24">
        <!-- Available Webparts -->
        <el-col :span="8">
          <el-card shadow="never" class="webpart-panel">
            <template #header><span>사용 가능한 웹파트</span></template>
            <div
              v-for="wp in availableWebparts"
              :key="wp.type"
              class="webpart-item available"
              @click="addWebpart(wp)"
            >
              <el-icon><Plus /></el-icon>
              <div>
                <div class="webpart-title">{{ wp.title }}</div>
                <div class="webpart-desc">{{ wp.description }}</div>
              </div>
            </div>
            <el-empty v-if="availableWebparts.length === 0" description="모든 웹파트가 추가됨" :image-size="60" />
          </el-card>
        </el-col>

        <!-- Active Webparts -->
        <el-col :span="16">
          <el-card shadow="never" class="webpart-panel">
            <template #header>
              <span>활성 웹파트</span>
              <span style="color: #999; font-size: 12px; margin-left: 8px;">드래그하거나 화살표로 순서를 변경하세요</span>
            </template>
            <el-empty v-if="activeWebparts.length === 0" description="웹파트를 추가해 주세요" />
            <draggable
              v-model="activeWebparts"
              item-key="widgetId"
              handle=".drag-handle"
              animation="200"
            >
              <template #item="{ element, index }">
                <div class="webpart-item active">
                  <el-icon class="drag-handle"><DCaret /></el-icon>
                  <div style="flex: 1;">
                    <div class="webpart-title">{{ element.title }}</div>
                    <div style="display: flex; gap: 8px; margin-top: 4px; align-items: center;">
                      <el-select v-model="element.size" size="small" style="width: 120px;">
                        <el-option label="작게 (1/4)" value="small" />
                        <el-option label="보통 (1/2)" value="medium" />
                        <el-option label="크게 (3/4)" value="large" />
                        <el-option label="전체 (1/1)" value="full" />
                      </el-select>
                      <el-switch v-model="element.isVisible" size="small" active-text="표시" inactive-text="숨김" />
                    </div>
                  </div>
                  <div style="display: flex; flex-direction: column; gap: 4px;">
                    <el-button size="small" :icon="ArrowUp" circle :disabled="index === 0" @click="moveUp(index)" />
                    <el-button size="small" :icon="ArrowDown" circle :disabled="index === activeWebparts.length - 1" @click="moveDown(index)" />
                  </div>
                  <el-button size="small" type="danger" :icon="Delete" circle @click="removeWebpart(index)" />
                </div>
              </template>
            </draggable>
          </el-card>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Plus, Delete, ArrowUp, ArrowDown, DCaret } from '@element-plus/icons-vue'
import draggable from 'vuedraggable'
import apiClient from '@/shared/api/apiClient'

const router = useRouter()
const saving = ref(false)

interface WebpartDef {
  type: string
  title: string
  description: string
}

interface ActiveWebpart {
  widgetId?: string
  type: string
  title: string
  size: 'small' | 'medium' | 'large' | 'full'
  isVisible: boolean
  order: number
}

const ALL_WEBPART_DEFS: WebpartDef[] = [
  { type: 'approval_summary', title: '결재 현황', description: '내 결재 요약 현황 표시' },
  { type: 'recent_board', title: '최근 게시글', description: '최근 게시판 글 목록' },
  { type: 'calendar_today', title: '오늘의 일정', description: '오늘 일정 목록' },
  { type: 'attendance_status', title: '근태 현황', description: '이번 달 근태 요약' },
  { type: 'organization_chart', title: '조직도', description: '부서 조직도 미니 뷰' },
  { type: 'notice', title: '공지사항', description: '최근 공지사항 목록' },
  { type: 'quick_links', title: '빠른 링크', description: '자주 사용하는 메뉴 바로가기' },
  { type: 'my_todo', title: '내 할일', description: '개인 업무 할일 목록' },
]

const activeWebparts = ref<ActiveWebpart[]>([])

const activeTypes = computed(() => new Set(activeWebparts.value.map(w => w.type)))
const availableWebparts = computed(() =>
  ALL_WEBPART_DEFS.filter(d => !activeTypes.value.has(d.type))
)

function addWebpart(def: WebpartDef) {
  activeWebparts.value.push({
    type: def.type,
    title: def.title,
    size: 'medium',
    isVisible: true,
    order: activeWebparts.value.length + 1,
  })
}

function removeWebpart(index: number) {
  activeWebparts.value.splice(index, 1)
}

function moveUp(index: number) {
  if (index > 0) {
    const arr = activeWebparts.value
    ;[arr[index - 1], arr[index]] = [arr[index], arr[index - 1]]
  }
}

function moveDown(index: number) {
  const arr = activeWebparts.value
  if (index < arr.length - 1) {
    ;[arr[index], arr[index + 1]] = [arr[index + 1], arr[index]]
  }
}

async function loadLayout() {
  try {
    const res = await apiClient.get('/portal/webparts')
    const webparts: any[] = res.data.data ?? []
    activeWebparts.value = webparts
      .sort((a: any, b: any) => a.order - b.order)
      .map((w: any) => ({
        widgetId: w.widgetId,
        type: w.widgetType,
        title: ALL_WEBPART_DEFS.find(d => d.type === w.widgetType)?.title ?? w.widgetType,
        size: w.size ?? 'medium',
        isVisible: w.isVisible ?? true,
        order: w.order,
      }))
  } catch {
    // first time — empty layout
  }
}

async function saveLayout() {
  saving.value = true
  try {
    const payload = activeWebparts.value.map((w, idx) => ({
      widgetId: w.widgetId,
      widgetType: w.type,
      size: w.size,
      isVisible: w.isVisible,
      order: idx + 1,
    }))
    await apiClient.put('/portal/webparts', { webparts: payload })
    ElMessage.success('포털 레이아웃이 저장되었습니다.')
    router.push('/portal')
  } catch {
    ElMessage.error('저장에 실패했습니다.')
  } finally {
    saving.value = false
  }
}

onMounted(loadLayout)
</script>

<style scoped>
.portal-settings { max-width: 1200px; margin: 0 auto; }
.webpart-panel { height: 100%; min-height: 500px; }
.webpart-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  margin-bottom: 8px;
  background: #fff;
}
.webpart-item.available { cursor: pointer; transition: background 0.2s; }
.webpart-item.available:hover { background: #f0f5ff; border-color: #1890ff; }
.webpart-item.active { background: #fafafa; }
.webpart-title { font-weight: 600; font-size: 14px; }
.webpart-desc { color: #999; font-size: 12px; margin-top: 2px; }
.drag-handle { cursor: grab; font-size: 18px; color: #bbb; }
.drag-handle:active { cursor: grabbing; }
</style>
