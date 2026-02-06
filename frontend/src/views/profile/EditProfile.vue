<template>
    <section class="flex-wrap-row-justify-space-evenly">
        <FormSchema v-for = "data in schemas":data="data"/>
    </section>
</template>

<script lang="ts" setup>
import { computed } from 'vue';


const personalData = {
    method: "POST",
    encrypted: true,
    novalidate: true,
    action: "/profile",
    name: "profile-schema",
    title: "Profile Information",
    inputControl: 
    [
        
        { value: '', id: 'city', name: 'Kommune', placeholder: 'e.g Ålesund', cls: ['city-input'], autofocus: true },
        { value: '', id: 'county', name: 'Fylke', placeholder: 'e.g Møre og Romsdal', cls: ['county-input'], autofocus: true},
        { value: '', id: 'email', name: 'Email', placeholder: 'e.g ola.norman@outlook.com', cls: ['email-input'], autofocus: true },
        { value: '', id: 'bio', name: 'Biografi', placeholder: 'e.g Møre og Romsdal', cls: ['county-input'], type: 'textarea', autofocus: true}
    ]
}

const interestData = {
    method: "POST",
    encrypted: true,
    novalidate: true,
    action: "/profile",
    name: "profile-schema",
    title: "Dine interesser",
    inputControl: 
    [
        { value: '', id: 'intrest', name: 'interest-tag', placeholder: 'e.g C#, Python, Java...', cls: ['interest-input'], autofocus: true },
    ]
}
const schemas = computed(() => {
    return [
        personalData,
        interestData
    ]
});
</script>