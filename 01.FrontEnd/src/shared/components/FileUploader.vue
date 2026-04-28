<template>
  <div class="file-uploader">
    <el-upload
      :action="uploadUrl"
      :headers="uploadHeaders"
      :multiple="multiple"
      :limit="limit"
      :accept="accept"
      :on-success="onSuccess"
      :on-error="onError"
      :on-remove="onRemove"
      :file-list="fileList"
      :before-upload="beforeUpload"
      :disabled="disabled"
      drag
      style="width:100%;"
    >
      <el-icon style="font-size:28px;"><Upload /></el-icon>
      <div style="margin-top:8px;">파일을 끌어놓거나 클릭하여 업로드</div>
      <template #tip>
        <div style="font-size:12px; color:#999; margin-top:4px;">
          최대 {{ maxSizeMb }}MB · 허용: {{ accept || '전체' }}
        </div>
      </template>
    </el-upload>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { Upload } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores/auth'

export interface UploadedFile {
  id: string
  originalName: string
  contentType: string
  fileSize: number
}

const props = withDefaults(defineProps<{
  modelValue: UploadedFile[]
  entityType?: string
  entityId?: string
  multiple?: boolean
  limit?: number
  accept?: string
  maxSizeMb?: number
  disabled?: boolean
}>(), {
  multiple: true,
  limit: 10,
  accept: '.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.hwp,.jpg,.jpeg,.png,.zip',
  maxSizeMb: 100,
  disabled: false,
})

const emit = defineEmits<{
  'update:modelValue': [files: UploadedFile[]]
}>()

const authStore = useAuthStore()

const uploadUrl = computed(() => {
  const base = (import.meta as any).env?.VITE_API_BASE_URL || '/api'
  const params = new URLSearchParams()
  if (props.entityType) params.set('entityType', props.entityType)
  if (props.entityId) params.set('entityId', props.entityId)
  return `${base}/files/upload?${params.toString()}`
})

const uploadHeaders = computed(() => ({
  Authorization: authStore.accessToken ? `Bearer ${authStore.accessToken}` : '',
}))

const fileList = ref<{ name: string; uid: number }[]>([])

function beforeUpload(file: File) {
  const sizeMb = file.size / 1024 / 1024
  if (sizeMb > props.maxSizeMb) {
    ElMessage.error(`파일 크기는 최대 ${props.maxSizeMb}MB 입니다.`)
    return false
  }
  return true
}

function onSuccess(response: { data: UploadedFile }) {
  if (response?.data) {
    emit('update:modelValue', [...props.modelValue, response.data])
    ElMessage.success(`${response.data.originalName} 업로드 완료`)
  }
}

function onError(_err: Error, file: { name: string }) {
  ElMessage.error(`${file.name} 업로드 실패`)
}

function onRemove(_file: unknown, _fileList: unknown[]) {
  // 서버에서 실제 삭제하려면 apiClient.delete 호출 필요
  // 여기서는 목록만 업데이트
}
</script>
