<template>
    <form
        class = "form-container flex-wrap-column"
        :method="data.method"     
        :name="data.name"
        :action="data.action"
        :rel="data.rel? data.rel : 'noopener'"
        :target="data.target? data.target : '_self'"
        :novalidate="data.novalidate? data.novalidate : false"
        v-on:encrypted="data.encrypted? data.encrypted : false"
        :autocomplete="data.autocomplete? data.autocomplete : 'off'"
        :acceptcharset="data.acceptcharset? data.acceptcharset : 'UTF-8'">
        <legend v-if = "data.title" class="form-title">
            <h1>{{ data.title }}</h1>
        </legend>
        <section v-if="isInput" v-for="field in data.inputControl" :key="field.id">
            <label :for="field.name">{{ field.name }} :</label>
            <FormInputs :data="field" v-model="schemaData[field.name]" />
        </section>
        <section v-if="isSelections" v-for="selection in data.selections" :key="selection.id">
            <label :for="selection.label">{{ selection.label }}</label>
            <select v-model="schemaData[selection.id]"
                    :multiple="selection.multiple || false">
                <option v-for="option in (selection.selectOptions || [])" :key="option.id" :value="option.value">
                    {{ option.label }}
                </option>
            </select>
        </section>
        <section v-if="isTextArea && data.textarea && data.textarea.name">
            <label :for="data.textarea.name">{{ data.textarea.label }}</label>
            <textarea
                :id="data.textarea.id"
                v-model="schemaData[data.textarea.name]"
                :placeholder="data.textarea.placeholder"
                :rows="data.textarea.rows || 4"
                :cols="data.textarea.cols || 50"
                :maxlength="data.textarea.maxlength || undefined"
                :required="!!data.textarea.required">
            </textarea>
        </section>
        <section v-if="isDataList && Array.isArray(data.datalist) && data.datalist.length">
            <label v-if="data.datalist[0]?.name" :for="data.datalist[0].name">{{ data.datalist[0].label }}</label>
            <input
                v-if="data.datalist[0]?.name"
                :id="data.datalist[0].id"
                v-model="schemaData[data.datalist[0].name]"
                :list="data.datalist[0].id"
                :placeholder="data.datalist[0].placeholder"
                :required="!!data.datalist[0].required" />
            <datalist :id="data.datalist[0]?.id">
                <option v-for="option in data.datalist" :key="option.id" :value="option.value">
                    {{ option.label }}
                </option>
            </datalist>
        </section>
        <section v-if="isOutputs && Array.isArray(data.outputs) && data.outputs.length">
            <label v-for="output in data.outputs" :for="output.name" :key="output.id">{{ output.label }}</label>
            <output
                v-for="output in data.outputs"
                :key="output.id"
                :id="output.id"
                :for="output.for"
                :v-model="schemaData[output.name]">
            </output>
        </section>
        <section  class="flex-wrap-row">
            <NavigationButton v-for="btn in buttons" :data="btn" />
        </section>
    </form>
</template>
<script lang="ts" setup>

    //  --- Importing Dependencies & Types
    import { computed, ref } from 'vue';
    import type { FormProps, SelectionItem } from '@/types/form'

    //  --- Props Definition Logic
    const props = defineProps<FormProps>();
    const data = computed(() => props.data);

    //  --- Flag Logic
    const isOutputs = computed<boolean>(() => Array.isArray(data.value.outputs) && data.value.outputs.length > 0);
    const isTextArea = computed<boolean>(() => !!data.value.textarea);
    const isDataList = computed<boolean>(() => Array.isArray(data.value.datalist) && data.value.datalist.length > 0);
    const isInput = computed<boolean>(() => !!data.value.inputControl);
    const isSelections = computed<boolean>(() => Array.isArray(data.value.selections) && data.value.selections.length > 0);

    //  Form Logic
    const schemaData = ref<Record<string, any>>({});
    const buttons = [
        {
            type : "submit",
            cls : "submit-btn",
            label : "submit",
            action : () => {}
            
        },
        {
            type : "reset",
            cls : "reset-btn",
            label : "Reset",
            action : () => {}
        },
    ]
    //  Debug Logic
    console.log("Data passed to Schema :",data.value);

</script>