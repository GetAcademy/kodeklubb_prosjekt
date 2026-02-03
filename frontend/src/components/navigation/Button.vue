<template>
    <button
        :class="cls"
        @click="data.action()"
        :type="!!data.type ? data.type : 'button'"
        :disabled="data.disabled">
        
        {{ data.label }}
    </button>
</template>

<script lang="ts" setup>

    //  --- Importing Dependencies & Types
    import { computed } from 'vue';
    import type { ButtonProps } from '@/types/navigation/buttons';

    //  --- Define Props Logic
    const props = defineProps<ButtonProps>();
    const data = computed(() => props.data);
    const cls = computed(() => props.cls ?? ['btn']);

    //  --- Debug logic
    // console.log("Buttons - Data : ", data.value)
    // console.log("Buttons - Class Data : ", cls.value)
</script>