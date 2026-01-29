<template>
    <nav :class="cls[0]">
        <ul :class="cls[1]">
            <li v-for="(item, i) in data" :key="i"
                :class="cls[2]">

                   <RouterLink v-if="!!isRouterLink"
                        :to="item.path"
                        :class="[{ 'active': $route.path === item.path}, cls[3]]">
                        {{ item.label }}
                    </RouterLink>
                    
                    <NavigationAnchor v-if="!!isAnchor && !!item.anchor"
                        :data="item.anchor"
                        :cls="[cls[3]]"/>

                    <NavigationButton v-if="!!isButton && !!item.action"
                        :data="item"
                        :cls="[cls[3]]" />
            </li>
        </ul>
    </nav>
</template>

<script lang="ts" setup>

    //  --- Importing Dependencies & Types
    import { computed } from 'vue';

    import type { NavigationProp } from '@/types/navigation/navigation';


    //  --- Props Definition Logic
    const props = withDefaults(defineProps<NavigationProp>(),
    {
        data: () => [],
        cls: () => [['nav-bar', 'flex-wrap-row-justify-space-between'],
                ['nav-list', 'flex-wrap-row-align-items-center'],
                ['nav-item'],
                ['nav-link']]
        });

    const cls = computed(() => props.cls);
    const data = computed(() => props.data);

    const isButton = computed(() => { return !!props.data.find(item => item.type == 'button')});
    const isAnchor = computed(() => { return !!props.data.find(item => item.type == 'anchor')});
    const isRouterLink = computed(() => { return !!props.data.find(item => item.type == 'router' )});
    

    //  --- Debug logic
    //console.error('NavMenu props data:', props.data);
    //console.log('isRouterLink:', isRouterLink.value);
    //console.log('isAnchor:', isAnchor.value);
</script>