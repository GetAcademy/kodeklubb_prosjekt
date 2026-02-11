<template>
    <nav v-if="data && data.length > 0" :class="cls[0]">
        <ul :class="cls[1]">
            <li v-for="(item, i) in data" :key="i"
                :class="[cls[2]]">

                   <RouterLink v-if="!!isRouterLink"
                        :to="item.path"
                        v-slot="{ navigate }"
                        :class="[{ 'active': $route.path === item.path}]">
                        <span @click="item.action ? item.action(navigate) : navigate()">{{ item.label }}</span>
                    </RouterLink>

                    <NavigationAnchor v-if="!!isAnchor && !!item.anchor"
                        :data="item.anchor"
                        :cls="[cls[3]]"/>

                    <NavigationButton v-if="!!isButton"
                        :data="item.data"
                        :cls="[item.cls]"/>
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
        cls: () => [['nav-bar'],
                ['nav-list', 'flex-wrap-row-align-content-start-justify-space-evenly'],
                ['nav-item'],
                ['nav-link']]
    });

    const cls = computed(() => props.cls);
    const data = computed<NavigationProp['data']>(() => props.data || []);

    //  --  State Logic
    const isButton = computed<boolean>(() => { return !!(data.value && data.value.some(item => item.type == 'button'))});
    const isAnchor = computed<boolean>(() => { return !!(data.value && data.value.some(item => item.type == 'anchor'))});
    const isRouterLink = computed<boolean>(() => { return !!(data.value && data.value.some(item => item.type == 'router' ))});
    

    //  --- Debug logic
    //console.log('isAnchor:', isAnchor.value);
    //console.error('NavMenu props data:', props.data);
    //console.log('isRouterLink:', isRouterLink.value);
</script>