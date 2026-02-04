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
            <select v-model="formData.selectedOption" 
                    :multiple="selection.multiple ? selection.multiple : false">
                <option v-for="option in data.selectOptions" :key="option.id" :value="option.value">
                    {{ option.label }}
                </option>
            </select>
        </section>
        <section v-if="isTextArea">
            <label :for="data.textarea.name">{{ data.textarea.label }}</label>
            <textarea 
                :id="data.textarea.id"
                v-model="formData[data.textarea.name]"
                :placeholder="data.textarea.placeholder"
                :rows="data.textarea.rows ? data.textarea.rows : 4"
                :cols="data.textarea.cols ? data.textarea.cols : 50"
                :maxlength="data.textarea.maxlength ? data.textarea.maxlength : ''"
                :required="data.textarea.required ? data.textarea.required : false">
            </textarea>
        </section>
        <section v-if="isDataList">
            <label :for="data.dataList.name">{{ data.dataList.label }}</label>
            <input 
                :id="data.dataList.id"
                v-model="formData[data.dataList.name]"
                :list="data.dataList.list"
                :placeholder="data.dataList.placeholder"
                :required="data.dataList.required ? data.dataList.required : false" />
            <datalist :id="data.dataList.list">
                <option v-for="option in data.dataList.options" :key="option.id" :value="option.value">
                    {{ option.label }}
                </option>
            </datalist>
        </section>
        <section v-if="isOutputs">
            <label :for="data.outputs.name">{{ data.outputs.label }}</label>
            <output 
                :id="data.outputs.id"
                :for="data.outputs.for"
                :v-model="formData[data.outputs.name]">
            </output>
        </section>
        <section  class="flex-wrap-row">
            <NavigationButton v-for="btn in buttons" :data="btn" />
        </section>
    </form>
</template>
<script lang="ts" setup>

    //  --- Importing Dependencies & Types
    import { computed } from 'vue';
    import type { FormProps } from '@/types/form'

    //  --- Props Definition Logic
    const props = defineProps<FormProps>();
    const data = computed(() => props.data);

    //  --- Flag Logic
    const isOutputs = computed<boolean>(() => !!data.value.outputs);
    const isTextArea = computed<boolean>(() => !!data.value.textarea);
    const isDataList = computed<boolean>(() => !!data.value.datalist);
    const isInput = computed<boolean>(() => !!data.value.inputControl);
    const isSelections = computed<boolean>(() => !!data.value.selections);

    //  Form Logic
    let schemaData = {}
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