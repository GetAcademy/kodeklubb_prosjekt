<template>
        :id="data.id"
        :class="cls[1]"
        :name="data.name"
        :placeholder="!!data.placeholder ? data.placeholder : ''"
        :min="data.type =='range' ? !!data.rangeMin? data.rangeMin : 0 : ''"
        :step="data.type =='range' ? 1 : 0"
        :max="data.type =='range' ? !!data.rangeMax? data.rangeMax : 100 : ''"
        :value="!!data.value ? data.value : ''"
        :type="data.type ? data.type : 'text'"
        :size="!!data.size ? data.size : '30'"
        :width="!!data.width ? data.width : ''"
        :height="!!data.height ? data.height : ''"
        :pattern="!!data.pattern ? data.pattern : ''"
        :readonly="!!data.readonly ? data.readonly : false"
        :required="!!data.required ? data.required : false"
        :disabled="!!data.disabled ? data.disabled : false"
        :maxlength="!!data.maxlength ? data.maxlength : ''"
        :minlength="!!data.minlength ? data.minlength : ''"
        :autofocus="!!data.autofocus ? data.autofocus : false"
        :multiple="!!data.multiple && data.type == 'file' ? data.multiple : false"
        @input="emit('update:modelValue', $event.target.value)"
    />
    <label v-if="data.label" :for="data.name" :class="cls[0]">{{ data.label || data.name }}</label>
    <input
        :id="data.id"
        :class="cls[1]"
        :name="data.name"
        :placeholder="data.placeholder || ''"
        :min="data.type === 'range' ? (data.rangeMin ?? 0) : undefined"
        :step="data.type === 'range' ? 1 : undefined"
        :max="data.type === 'range' ? (data.rangeMax ?? 100) : undefined"
        :value="data.value || ''"
        :type="data.type || 'text'"
        :size="data.size || '30'"
        :width="data.width || undefined"
        :height="data.height || undefined"
        :pattern="data.pattern || undefined"
        :readonly="data.readonly || false"
        :required="data.required || false"
        :disabled="data.disabled || false"
        :maxlength="data.maxlength || undefined"
        :minlength="data.minlength || undefined"
        :autofocus="data.autofocus || false"
        :multiple="data.multiple && data.type === 'file' ? data.multiple : false"
        @input="emit('update:modelValue', ($event.target as HTMLInputElement)?.value)"
    />
</template>
<script lang="ts" setup>

    //  --- Dependencies & Types
    import { computed } from 'vue';
    import type { InputsProps } from '@/types/form'

    //  --- Props Definition Logic
    const props = defineProps<InputsProps>();
    const cls = computed(() => props.data.cls);
    const data = computed(() => props.data);
    const emit = defineEmits(['update:modelValue']);

    //  --- Debug logic
    //console.warn("Inputs.vue : ", data);
</script>