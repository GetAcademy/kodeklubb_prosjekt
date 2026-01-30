export interface FigureItem
{
    type    : string;
    src     : string;
    alt     : string;
    id?     : string;
    srcset? : string;
    caption? : string;
}

export interface FigureProps
{
    cls?   : string[];
    data    : FigureItem;
}

export interface iconProps
{
    cls?: string[];
    label?: string;
}