<template>
  <el-container style="height: 100vh">
    <!-- Sidebar -->
    <el-aside :width="sidebarCollapsed ? '64px' : '220px'" style="transition: width 0.3s; background: #001529;">
      <div class="logo" @click="sidebarCollapsed = !sidebarCollapsed">
        <span v-if="!sidebarCollapsed" style="color: white; font-size: 18px; font-weight: bold;">🏢 그룹웨어</span>
        <span v-else style="color: white; font-size: 20px;">🏢</span>
      </div>
      <el-menu
        :default-active="$route.path"
        router
        :collapse="sidebarCollapsed"
        background-color="#001529"
        text-color="#ffffffa6"
        active-text-color="#1890ff"
      >
        <el-menu-item index="/portal"><el-icon><House /></el-icon><template #title>포털</template></el-menu-item>
        <el-menu-item index="/approval"><el-icon><Finished /></el-icon><template #title>전자결재</template></el-menu-item>
        <el-menu-item index="/board"><el-icon><Memo /></el-icon><template #title>게시판</template></el-menu-item>
        <el-sub-menu index="calendar-group">
          <template #title><el-icon><Calendar /></el-icon><span>캘린더</span></template>
          <el-menu-item index="/calendar">캘린더</el-menu-item>
          <el-menu-item index="/calendar/resources">자원 예약</el-menu-item>
        </el-sub-menu>
        <el-menu-item index="/attendance"><el-icon><Timer /></el-icon><template #title>근태관리</template></el-menu-item>
        <el-menu-item v-if="isAdmin" index="/organizations"><el-icon><OfficeBuilding /></el-icon><template #title>조직관리</template></el-menu-item>
        <el-menu-item v-if="isAdmin" index="/admin"><el-icon><Setting /></el-icon><template #title>관리자</template></el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <!-- Header -->
      <el-header style="background: #fff; border-bottom: 1px solid #e8e8e8; display: flex; align-items: center; justify-content: flex-end; gap: 12px;">
        <el-badge :value="unreadCount" :max="99" :hidden="unreadCount === 0">
          <el-button :icon="Bell" circle @click="$router.push('/notifications')" />
        </el-badge>
        <el-dropdown @command="handleCommand">
          <div style="cursor: pointer; display: flex; align-items: center; gap: 8px;">
            <el-avatar :size="32" :src="user?.profileImageUrl">{{ user?.name?.charAt(0) }}</el-avatar>
            <span>{{ user?.name }}</span>
            <span style="color: #999; font-size: 12px;">{{ user?.departmentName }}</span>
          </div>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">로그아웃</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-header>

      <!-- Main content -->
      <el-main style="background: #f0f2f5; padding: 20px; overflow-y: auto;">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import { Bell, Setting } from '@element-plus/icons-vue'
import apiClient from '@/shared/api/apiClient'

const auth = useAuthStore()
const router = useRouter()
const sidebarCollapsed = ref(false)
const unreadCount = ref(0)

const user = computed(() => auth.user)
const isAdmin = computed(() => ['SystemAdmin', 'OrgAdmin', 'GroupwareAdmin'].includes(user.value?.systemRole || ''))

async function fetchUnreadCount() {
  try {
    const res = await apiClient.get('/notifications/unread-count')
    unreadCount.value = res.data.data ?? 0
  } catch { /* ignore */ }
}

function handleCommand(cmd: string) {
  if (cmd === 'logout') {
    auth.logout().then(() => router.push('/login'))
  }
}

onMounted(() => {
  fetchUnreadCount()
  setInterval(fetchUnreadCount, 30000)
})
</script>

<style scoped>
.logo {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  border-bottom: 1px solid rgba(255,255,255,0.1);
}
</style>
