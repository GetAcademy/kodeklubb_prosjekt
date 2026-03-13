export interface SelectionOption {
    id: string;
    label: string;
    value: string;
}

export interface SelectionItem {
    id: string;
    label: string;
    multiple?: boolean;
    selectOptions?: SelectionOption[];
}

export interface DataListOption {
    id: string;
    label: string;
    value: string;
    name?: string;
    placeholder?: string;
    required?: boolean;
}

export interface OutputItem {
    id: string;
    label: string;
    value: string;
    name: string;
    for?: string;
}

export interface TextAreaItem {
    id: string;
    label: string;
    name: string;
    placeholder?: string;
    rows?: number;
    cols?: number;
    maxlength?: number;
    required?: boolean;
}

export interface FormItem {
    rel?: string;
    name: string;
    title?: string;
    action: string;
    target?: string;
    encrypted?: boolean;
    novalidate?: boolean;
    acceptcharset?: string;
    inputControl?: InputItem[];
    autocomplete?: 'on' | 'off';
    outputs?: OutputItem[];
    textarea?: TextAreaItem;
    datalist?: DataListOption[];
    selections?: SelectionItem[];
    method: 'POST' | 'GET' | 'PUT' | 'DELETE' | 'PATCH';
};

export interface FormProps { data: FormItem; };
export interface InputItem
{
    id: string;
    name: string;
    value: string;
    size?: string;
    type?: 'password';
    width?: number;
    height?: string;
    pattern?: string;
    rangeMin?: number;
    rangeMax?: number;
    maxlength?: number;
    minlength?: number;
    disabled?: boolean;
    readonly?: boolean;
    rangeStep?: number;
    required?: boolean;
    autofocus?: boolean;
    multiple? : boolean;
    placeholder: string;
}
export interface InputsProps { data: inputItem; cls: string;}