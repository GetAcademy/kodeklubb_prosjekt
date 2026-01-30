<template>
    <button
        @click="data.action()"
        :type="data.type ?? 'button'"
        :disabled="data.disabled">
        
        {{ data.label }}

    </button>
</template>

<script lang="ts" setup>

    import { computed } from 'vue';
    import type { ButtonProps } from '@/types/navigation/buttons';

    const props = defineProps<ButtonProps>();
    const data = computed(() => props.data);

</script>