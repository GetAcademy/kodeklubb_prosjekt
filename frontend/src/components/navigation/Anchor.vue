<template>
    <a :href="data.href" :class="cls">
        <span v-if="data.label">{{ data.label }}</span>
    </a>
</template>

<script lang="ts" setup>
    import type { AnchorProps } from '@/types/navigation/navigation';

    const props = withDefaults(defineProps<AnchorProps>(), {
        cls: () => ['nav-link']
    });

    const data = props.data;
    const cls = props.cls;
</script>
