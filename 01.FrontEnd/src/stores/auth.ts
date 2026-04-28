import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiClient from '@/shared/api/apiClient'

export interface UserInfo {
  userId: string
  name: string
  email: string
  departmentId: string
  departmentName: string
  position: string
  systemRole: string
  profileImageUrl?: string
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<UserInfo | null>(null)
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isAuthenticated = computed(() => !!accessToken.value)

  async function login(email: string, password: string) {
    const res = await apiClient.post('/auth/login', { email, password })
    const data = res.data.data
    accessToken.value = data.accessToken
    refreshToken.value = data.refreshToken
    user.value = {
      userId: data.userId,
      name: data.name,
      email: data.email,
      departmentId: data.departmentId,
      departmentName: data.departmentName,
      position: data.position,
      systemRole: data.systemRole,
      profileImageUrl: data.profileImageUrl,
    }
    localStorage.setItem('accessToken', data.accessToken)
    localStorage.setItem('refreshToken', data.refreshToken)
    localStorage.setItem('user', JSON.stringify(user.value))
  }

  async function logout() {
    try {
      await apiClient.post('/auth/logout')
    } finally {
      accessToken.value = null
      refreshToken.value = null
      user.value = null
      localStorage.removeItem('accessToken')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('user')
    }
  }

  function loadFromStorage() {
    const stored = localStorage.getItem('user')
    if (stored) {
      try { user.value = JSON.parse(stored) } catch { /* ignore */ }
    }
  }

  return { user, accessToken, isAuthenticated, login, logout, loadFromStorage }
})
