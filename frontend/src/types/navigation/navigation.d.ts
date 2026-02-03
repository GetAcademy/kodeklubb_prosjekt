import type { FigureItem } from "../media";
import type { ButtonItem } from "./buttons";

export interface NavigationProp
{
    type: 'router' | 'anchor' | 'button';
    data: RouteItem[] | AnchorItem[] | ButtonItem[];
    cls?: Array<string | string[] | Array<string | string[]>>;
}

export interface AnchorItem
{
    href: string;
    label?: string;
    type: string[];
    img?: FigureItem;
}

export interface AnchorProps
{
    data: AnchorItem;
    cls?: string[];
}

export interface RouterItem
{
    type: 'router';
    path: string;
    label: string;
    action?: () => void;
}