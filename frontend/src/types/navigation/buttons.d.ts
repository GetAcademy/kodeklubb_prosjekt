export interface ButtonItem
{
    type?: 'submit' | 'reset';
    label: string;
    action: () => void;
    disabled?: boolean;
}

export interface ButtonProps { data: ButtonItem, cls?: string[] | string; }