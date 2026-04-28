<template>
  <el-form :model="formData" :label-width="labelWidth" v-if="schema && schema.fields">
    <template v-for="field in schema.fields" :key="field.key">
      <el-form-item
        :label="field.label"
        :required="field.required"
      >
        <!-- 텍스트 -->
        <el-input
          v-if="field.type === 'text'"
          v-model="formData[field.key]"
          :placeholder="field.placeholder"
          :disabled="readonly"
        />

        <!-- 텍스트 에어리어 -->
        <el-input
          v-else-if="field.type === 'textarea'"
          v-model="formData[field.key]"
          type="textarea"
          :rows="field.rows || 4"
          :placeholder="field.placeholder"
          :disabled="readonly"
        />

        <!-- 숫자 -->
        <el-input-number
          v-else-if="field.type === 'number'"
          v-model="formData[field.key]"
          :min="field.min"
          :max="field.max"
          :step="field.step || 1"
          :disabled="readonly"
          style="width:100%;"
        />

        <!-- 금액 -->
        <el-input-number
          v-else-if="field.type === 'amount'"
          v-model="formData[field.key]"
          :min="0"
          :step="1000"
          :disabled="readonly"
          style="width:100%;"
          :formatter="(v: number) => `₩ ${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
        />

        <!-- 날짜 -->
        <el-date-picker
          v-else-if="field.type === 'date'"
          v-model="formData[field.key]"
          type="date"
          value-format="YYYY-MM-DD"
          :placeholder="field.placeholder || '날짜 선택'"
          :disabled="readonly"
          style="width:100%;"
        />

        <!-- 날짜 범위 -->
        <el-date-picker
          v-else-if="field.type === 'daterange'"
          v-model="formData[field.key]"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="~"
          start-placeholder="시작일"
          end-placeholder="종료일"
          :disabled="readonly"
          style="width:100%;"
        />

        <!-- 셀렉트박스 -->
        <el-select
          v-else-if="field.type === 'select'"
          v-model="formData[field.key]"
          :placeholder="field.placeholder || '선택'"
          :disabled="readonly"
          style="width:100%;"
        >
          <el-option
            v-for="opt in field.options"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>

        <!-- 라디오 -->
        <el-radio-group
          v-else-if="field.type === 'radio'"
          v-model="formData[field.key]"
          :disabled="readonly"
        >
          <el-radio
            v-for="opt in field.options"
            :key="opt.value"
            :value="opt.value"
          >{{ opt.label }}</el-radio>
        </el-radio-group>

        <!-- 체크박스 -->
        <el-checkbox-group
          v-else-if="field.type === 'checkbox'"
          v-model="formData[field.key]"
          :disabled="readonly"
        >
          <el-checkbox
            v-for="opt in field.options"
            :key="opt.value"
            :value="opt.value"
          >{{ opt.label }}</el-checkbox>
        </el-checkbox-group>

        <!-- 테이블형 반복 입력 -->
        <div v-else-if="field.type === 'table'">
          <el-table :data="formData[field.key] || []" border size="small" style="width:100%">
            <el-table-column
              v-for="col in field.columns"
              :key="col.key"
              :label="col.label"
              :width="col.width"
            >
              <template #default="{ row }">
                <el-input v-if="!readonly && col.type === 'text'" v-model="row[col.key]" size="small" />
                <el-input-number v-else-if="!readonly && col.type === 'number'" v-model="row[col.key]" size="small" style="width:100%" />
                <span v-else>{{ row[col.key] }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="!readonly" label="" width="60">
              <template #default="{ $index }">
                <el-button size="small" type="danger" text @click="removeTableRow(field.key, $index)">삭제</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-button v-if="!readonly" size="small" style="margin-top:8px;" @click="addTableRow(field)">행 추가</el-button>
        </div>

        <!-- 기본: input -->
        <el-input
          v-else
          v-model="formData[field.key]"
          :placeholder="field.placeholder"
          :disabled="readonly"
        />
      </el-form-item>
    </template>
  </el-form>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue'

export interface FormFieldOptionItem {
  label: string
  value: string | number
}

export interface FormFieldSchema {
  key: string
  label: string
  type: string
  required?: boolean
  placeholder?: string
  rows?: number
  min?: number
  max?: number
  step?: number
  options?: FormFieldOptionItem[]
  columns?: { key: string; label: string; type?: string; width?: number }[]
  defaultValue?: unknown
}

export interface FormSchema {
  fields: FormFieldSchema[]
}

const props = defineProps<{
  schema: FormSchema | null
  modelValue: Record<string, unknown>
  readonly?: boolean
  labelWidth?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: Record<string, unknown>]
}>()

const formData = reactive<Record<string, unknown>>({ ...props.modelValue })

watch(() => props.modelValue, (v) => {
  Object.assign(formData, v)
}, { deep: true })

watch(formData, (v) => {
  emit('update:modelValue', { ...v })
}, { deep: true })

function addTableRow(field: FormFieldSchema) {
  if (!Array.isArray(formData[field.key])) formData[field.key] = []
  const newRow: Record<string, unknown> = {}
  field.columns?.forEach(c => { newRow[c.key] = c.type === 'number' ? 0 : '' })
  ;(formData[field.key] as Record<string, unknown>[]).push(newRow)
}

function removeTableRow(key: string, index: number) {
  if (Array.isArray(formData[key])) {
    ;(formData[key] as unknown[]).splice(index, 1)
  }
}
</script>
