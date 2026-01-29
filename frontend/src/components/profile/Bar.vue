<template>
    <aside>
        <section v-if="data">
            <picture>
            </picture>
        </section>
    </aside>
</template>

<script lang="ts" setup>

    //  --- Importing Deps, Types
    import { computed } from 'vue';

    interface BarProps
    {
        data: Record<string,string>
    }
    const props = defineProps<BarProps>();

    const data = computed(() => props.data);
    
</script>