import type { Anchor, RouteItem } from "./anchor";
import type { ButtonItem } from "./buttons";

export interface NavigationProp
{
    totalPage?: number;
    activePage?: number;
    data: RouteItem | Anchor[] | ButtonItem[];
    cls?: Array<string | string[] | Array<string | string[]>>;
}