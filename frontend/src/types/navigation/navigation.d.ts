import type { Anchor, RouteItem } from "./anchor";

export interface NavigationProp
{
    type: string;
    totalPage?: number;
    activePage?: number;
    data: RouteItem | Anchor[];
    cls?: Array<string | string[] | Array<string | string[]>>;
}