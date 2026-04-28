<template>
  <div>
    <h2 style="margin-bottom: 16px;">조직관리</h2>
    <el-row :gutter="16">
      <el-col :span="8">
        <el-card>
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <b>조직도</b>
              <el-button v-if="isAdmin" type="primary" size="small" @click="showOrgDialog = true; editOrg = null; orgForm = { name:'', code:'', description:'', parentId:'' }">+ 추가</el-button>
            </div>
          </template>
          <el-tree :data="treeData" :props="{ label: 'name', children: 'children' }" @node-click="onOrgClick" highlight-current default-expand-all>
            <template #default="{ node, data }">
              <span>{{ node.label }}</span>
              <span style="margin-left: 4px; color: #999; font-size: 11px;">{{ data.memberCount ? `(${data.memberCount})` : '' }}</span>
            </template>
          </el-tree>
        </el-card>
      </el-col>
      <el-col :span="16">
        <el-card v-if="selectedOrg">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <b>{{ selectedOrg.name }} 구성원</b>
              <div v-if="isAdmin">
                <el-button size="small" @click="editOrgOpen">수정</el-button>
                <el-button size="small" type="danger" @click="deleteOrg">삭제</el-button>
              </div>
            </div>
          </template>
          <el-table :data="members" v-loading="loadingMembers">
            <el-table-column prop="name" label="이름" width="100" />
            <el-table-column prop="email" label="이메일" />
            <el-table-column prop="position" label="직위" width="100" />
            <el-table-column prop="phone" label="연락처" width="140" />
            <el-table-column label="상태" width="80">
              <template #default="{ row }"><el-tag :type="row.isActive ? 'success' : 'info'" size="small">{{ row.isActive ? '재직' : '퇴직' }}</el-tag></template>
            </el-table-column>
          </el-table>
        </el-card>
        <el-card v-else><el-empty description="조직을 선택하세요" /></el-card>
      </el-col>
    </el-row>

    <el-dialog v-model="showOrgDialog" :title="editOrg ? '조직 수정' : '조직 추가'" width="450px">
      <el-form :model="orgForm" label-width="80px">
        <el-form-item label="조직명"><el-input v-model="orgForm.name" /></el-form-item>
        <el-form-item label="코드"><el-input v-model="orgForm.code" /></el-form-item>
        <el-form-item label="상위조직">
          <el-select v-model="orgForm.parentId" clearable style="width: 100%;">
            <el-option v-for="o in flatOrgs" :key="o.id" :label="o.name" :value="o.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="설명"><el-input v-model="orgForm.description" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showOrgDialog = false">취소</el-button>
        <el-button type="primary" @click="saveOrg" :loading="saving">저장</el-button>
      </template>
    </el-dialog>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import apiClient from '@/shared/api/apiClient'
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore(); const isAdmin = auth.user?.role === 'Admin'
const treeData = ref<Record<string,unknown>[]>([]); const flatOrgs = ref<Record<string,unknown>[]>([]); const members = ref<Record<string,unknown>[]>([]); const selectedOrg = ref<Record<string,unknown>|null>(null); const loadingMembers = ref(false)
const showOrgDialog = ref(false); const editOrg = ref<Record<string,unknown>|null>(null); const saving = ref(false)
const orgForm = ref({ name: '', code: '', description: '', parentId: '' })
async function fetchTree() { try { const r = await apiClient.get('/organizations/tree'); treeData.value = r.data.data ? [r.data.data] : [] } catch { /**/ } }
function flatten(nodes: Record<string,unknown>[], arr: Record<string,unknown>[] = []) { for (const n of nodes) { arr.push(n); if ((n.children as Record<string,unknown>[])?.length) flatten(n.children as Record<string,unknown>[], arr) } return arr }
const flatOrgsComputed = computed(() => flatten(treeData.value))
async function onOrgClick(data: Record<string,unknown>) {
  selectedOrg.value = data; loadingMembers.value = true
  try { const r = await apiClient.get('/users', { params: { departmentId: data.id, page:1, pageSize:100 } }); members.value = r.data.data?.items || [] } catch { members.value = [] } finally { loadingMembers.value = false }
}
function editOrgOpen() { editOrg.value = selectedOrg.value; if (selectedOrg.value) { orgForm.value = { name: selectedOrg.value.name as string, code: selectedOrg.value.code as string, description: (selectedOrg.value.description as string)||'', parentId: (selectedOrg.value.parentId as string)||'' } }; showOrgDialog.value = true }
async function saveOrg() {
  saving.value = true
  try {
    if (editOrg.value) { await apiClient.put(`/organizations/${editOrg.value.id}`, orgForm.value) }
    else { await apiClient.post('/organizations', orgForm.value) }
    ElMessage.success('저장되었습니다'); showOrgDialog.value = false; fetchTree()
  } catch { ElMessage.error('저장 실패') } finally { saving.value = false }
}
async function deleteOrg() {
  await ElMessageBox.confirm('삭제하시겠습니까?', '확인', { type: 'warning' })
  try { await apiClient.delete(`/organizations/${selectedOrg.value!.id}`); ElMessage.success('삭제되었습니다'); selectedOrg.value = null; fetchTree() } catch { ElMessage.error('삭제 실패') }
}
onMounted(() => { fetchTree() })
</script>
