export interface FormItem
{
    
    rel?:string;
    name: string;
    title: string;
    action:string;
    target?:string;
    encrypted?: boolean;
    novalidate?: boolean;
    autocomplete?: boolean;
    acceptcharset?: string;
    inputControl?: InputItem[];
    outputs?: Record<string,string>;
    textarea?: Record<string,string>;
    datalist?: Record<string,string>;
    selections?: Record<string,string>;
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