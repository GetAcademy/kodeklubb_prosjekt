export interface ButtonItem
{
    type?: 'button';
    label: string;
    action: () => void;
    disabled?: boolean;
    cls: string[] | string;
}

export interface ButtonProps { data: ButtonItem }