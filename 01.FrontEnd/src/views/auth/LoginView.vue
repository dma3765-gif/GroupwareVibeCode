<template>
  <div class="login-container">
    <el-card class="login-card">
      <div class="login-header">
        <h1>🏢 그룹웨어</h1>
        <p>기업 업무 통합 플랫폼</p>
      </div>
      <el-form :model="form" :rules="rules" ref="formRef" @submit.prevent="handleLogin">
        <el-form-item prop="email">
          <el-input v-model="form.email" placeholder="이메일" size="large" prefix-icon="User" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="form.password" type="password" placeholder="비밀번호" size="large"
            prefix-icon="Lock" show-password @keyup.enter="handleLogin" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="large" style="width: 100%;" :loading="loading" @click="handleLogin">
            로그인
          </el-button>
        </el-form-item>
      </el-form>
      <div class="demo-accounts">
        <p style="color: #999; font-size: 12px; text-align: center;">테스트 계정</p>
        <div class="demo-row" v-for="acc in demoAccounts" :key="acc.email" @click="setDemo(acc)">
          <el-tag size="small" :type="acc.type">{{ acc.role }}</el-tag>
          <span>{{ acc.email }} / {{ acc.password }}</span>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'

const router = useRouter()
const auth = useAuthStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({ email: '', password: '' })

const rules: FormRules = {
  email: [
    { required: true, message: '이메일을 입력하세요.', trigger: 'blur' },
    { type: 'email', message: '올바른 이메일 형식이 아닙니다.', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '비밀번호를 입력하세요.', trigger: 'blur' },
  ],
}

const demoAccounts = [
  { email: 'admin@groupware.com', password: 'Admin1234!', role: '시스템관리자', type: 'danger' as const },
  { email: 'hr@groupware.com',    password: 'User1234!',  role: '인사팀장',    type: 'warning' as const },
  { email: 'it@groupware.com',    password: 'User1234!',  role: 'IT팀장',      type: 'primary' as const },
  { email: 'user1@groupware.com', password: 'User1234!',  role: '일반사용자',  type: 'info' as const },
]

function setDemo(acc: typeof demoAccounts[0]) {
  form.email = acc.email
  form.password = acc.password
}

async function handleLogin() {
  await formRef.value?.validate()
  loading.value = true
  try {
    await auth.login(form.email, form.password)
    router.push('/')
  } catch (e: unknown) {
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    ElMessage.error(msg || '로그인에 실패했습니다.')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #1890ff 0%, #001529 100%);
  display: flex;
  align-items: center;
  justify-content: center;
}
.login-card {
  width: 420px;
  padding: 20px;
}
.login-header {
  text-align: center;
  margin-bottom: 24px;
}
.login-header h1 { font-size: 28px; color: #1890ff; margin: 0; }
.login-header p { color: #999; margin: 8px 0 0; }
.demo-accounts { margin-top: 16px; border-top: 1px solid #eee; padding-top: 12px; }
.demo-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 8px;
  cursor: pointer;
  border-radius: 4px;
  font-size: 12px;
  color: #666;
}
.demo-row:hover { background: #f5f5f5; }
</style>
