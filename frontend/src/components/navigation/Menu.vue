<template>
    <nav v-if="data && data.length > 0" :class="cls[0]">
        <ul :class="cls[1]">
            <li v-for="(item, i) in data" :key="i"
                :class="[cls[2]]">

                   <RouterLink v-if="isRouterItem(item)" :to="item.path" v-slot="{ navigate }" :class="[{ 'active': $route.path === item.path}]">
                        <span @click="item.action && typeof item.action === 'function' ? item.action() : navigate()">{{ item.label }}</span>
                    </RouterLink>

                    <NavigationAnchor v-if="isAnchorItem(item)"
                        :data="item"
                        :cls="cls[3] ? [cls[3]] : undefined"/>

                    <NavigationButton v-if="isButtonItem(item)"
                        :data="item"
                        :cls="'cls' in item && item.cls ? [item.cls] : undefined"/>
            </li>
        </ul>
    </nav>
</template>

<script lang="ts" setup>
    import type { RouterItem, AnchorItem } from '@/types/navigation/navigation';
    import type { ButtonItem } from '@/types/navigation/buttons';

    function isRouterItem(item: any): item is RouterItem {
        return item && item.type === 'router' && 'path' in item;
    }
    function isAnchorItem(item: any): item is AnchorItem {
        return item && item.type === 'anchor' && 'href' in item;
    }
    function isButtonItem(item: any): item is ButtonItem {
        return item && item.type === 'button' && 'label' in item && 'action' in item;
    }

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
    const isRouterLink = computed<boolean>(() => { return !!(data.value && data.value.some(item => item.type == 'router' ))});
    

    //  --- Debug logic
    //console.log('isAnchor:', isAnchor.value);
    //console.error('NavMenu props data:', props.data);
    //console.log('isRouterLink:', isRouterLink.value);
</script>