export class ConsoleLogHelper {
    public static writeLine(value: string): void {
        console.log(value);
    }

    public static writeError(value: string): void {
        console.log("%c" + value, "color: red;");
    }

    public static groupEnd(): void {
        console.groupEnd();
    }

    public static groupCollapsed(label: string): void {
        console.groupCollapsed("%c" + label, "color: red; font-weight: normal;");
    }
}
