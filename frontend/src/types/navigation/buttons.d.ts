export interface ButtonItem
{
    type: string,
    label: string,
    action: () => void,
}

export interface ButtonProps { data: ButtonItem}