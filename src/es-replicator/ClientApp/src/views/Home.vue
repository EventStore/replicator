<template>
  <div class="home">
<!--    <el-row :gutter="30">-->
<!--      <el-col :span="24">-->
        <Dashboard />
<!--      </el-col>-->
<!--    </el-row>-->
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import Dashboard from "@/components/Dashboard.vue";

export default defineComponent({
  name: "Home",
  components: {
    Dashboard,
  },
});
</script>
