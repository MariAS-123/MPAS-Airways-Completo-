<script setup>
import NavbarPublico from '@/components/NavbarPublico.vue'
import FooterApp from '@/components/FooterApp.vue'
</script>

<template>
  <div class="flex min-h-screen flex-col bg-background">
    <NavbarPublico />

    <main class="flex-1">
      <RouterView />
    </main>

    <FooterApp />
  </div>
</template>
