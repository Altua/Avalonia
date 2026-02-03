export class ConsoleLogHelper {
    private static readonly ERROR_STYLE = "color: light-dark(red, #FF6B6B); font-weight: normal;";

    public static writeLine(value: string): void {
        console.log(value);
    }

    public static writeError(value: string): void {
        console.log("%c" + value, ConsoleLogHelper.ERROR_STYLE);
    }

    public static groupEnd(): void {
        console.groupEnd();
    }

    public static groupCollapsed(label: string): void {
        console.groupCollapsed("%c" + label, ConsoleLogHelper.ERROR_STYLE);
    }
}
