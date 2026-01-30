export interface ButtonItem
{
    type: string,
    label: string,
    action: () => void,
    disabled?: boolean;
}

export interface ButtonProps { data: ButtonItem }