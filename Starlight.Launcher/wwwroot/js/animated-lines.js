// MIT - darkrell
registerPaint('animated-lines', class {
    static get inputProperties() {
        return [
            '--circle',
            '--dot-color',
            '--dot-size',
            '--num-points',
            '--line-color',
            '--line-opacity',
            '--dot-speed',
            '--distance',
            '--seed',
            '--x',
            '--y',
            'order'
        ];
    }

    paint(ctx, size, properties) {
        const circle = properties.get('--circle')?.toString() || false;
        const dotColor = properties.get('--dot-color')?.toString() || 'gray';
        const lineColor = properties.get('--line-color')?.toString() || 'gray';
        const lineOpacity = parseFloat(properties.get('--line-opacity')?.toString()) || 0;
        const lineSpeed = parseFloat(properties.get('--dot-speed')?.toString()) || 1;
        const xoff = parseFloat(properties.get('--x')?.toString()) || 0;
        const yoff = parseFloat(properties.get('--y')?.toString()) || 0;
        const dotSize = parseInt(properties.get('--dot-size')?.toString()) || 1;
        const numPoints = parseInt(properties.get('--num-points')?.toString()) || 50;
        const distanceProp = parseInt(properties.get('--distance')?.toString()) || 100;
        const seed = parseInt(properties.get('--seed')?.toString()) || 0;
        const frame = (parseInt(properties.get('order')?.toString()) || 0) * lineSpeed;
        const points = [];
        for (let i = 0; i < numPoints; i++) {
            const startX = deterministicRandom(seed + i * 4, 0, size.width);
            const startY = deterministicRandom(seed + i * 4 + 1, 0, size.height);
            const directionX = deterministicRandom(seed + i * 4 + 2, -1, 1) + xoff;
            const directionY = deterministicRandom(seed + i * 4 + 3, -1, 1) + yoff;

            if (circle) {
                let h = size.width / 2;
                let k = size.height / 2;
                let r = Math.hypot(h - startX, k - startY);
                let theta = (3600 + frame) * 0.02 * Math.abs(directionY + directionX);

                let x = r * Math.cos(theta) + h;
                let y = r * Math.sin(theta) + k;
                points.push({ x, y });
            } else {
                let x = (startX + directionX * frame) % size.width;
                let y = (startY + directionY * frame) % size.height;

                if (x < 0) x += size.width;
                if (y < 0) y += size.height;

                points.push({ x, y });
            }
        }

        ctx.fillStyle = dotColor;
        points.forEach(point => {
            ctx.beginPath();
            ctx.arc(point.x, point.y, dotSize, 0, 2 * Math.PI);
            ctx.fill();
        });
        if (lineOpacity == 0) return;
        ctx.strokeStyle = lineColor;
        ctx.globalAlpha = lineOpacity;
        ctx.lineWidth = 2;

        for (let i = 0; i < points.length; i++) {
            for (let j = i + 1; j < points.length; j++) {
                const distance = Math.hypot(points[i].x - points[j].x, points[i].y - points[j].y);
                if (distance < distanceProp) {
                    ctx.beginPath();
                    ctx.moveTo(points[i].x, points[i].y);
                    ctx.lineTo(points[j].x, points[j].y);
                    ctx.stroke();
                }
            }
        }
    }
});

function deterministicRandom(seed, min, max) {
    const x = Math.sin(seed) * 10000;
    return min + (x - Math.floor(x)) * (max - min);
}
