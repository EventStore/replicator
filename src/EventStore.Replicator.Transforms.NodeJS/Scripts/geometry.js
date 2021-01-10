import * as Arithmetic from './arithmetic.js';
import * as Self from 'geometry.js';

export const Meta = import.meta;

export class Rectangle {
    constructor(width, height) {
        this.width = width;
        this.height = height;
    }
    get Area() {
        return Self.Rectangle.CalculateArea(this.width, this.height);
    }
    static CalculateArea(width, height) {
        return Arithmetic.Multiply(width, height);
    }
}

export class Square extends Rectangle {
    constructor(side) {
        super(side, side);
    }
}