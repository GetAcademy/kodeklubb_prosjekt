<template>
    <button @click="data.action()"> {{ data.label }}</button>
</template>

<script lang="ts" setup>

    import { computed } from 'vue';
    import type { ButtonProps } from '@/types/navigation/button';

    const props = defineProps<ButtonProps>();
    const data = computed(() => props.data);
</script>