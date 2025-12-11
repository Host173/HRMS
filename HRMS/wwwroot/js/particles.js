const canvas = document.getElementById("particleCanvas");
if (canvas) {
    const ctx = canvas.getContext("2d");

    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;

    const drops = [];
    const dropCount = 400; 

    class Drop {
        constructor() {
            this.reset(true);
        }

        reset(initial = false) {
            // Increased angle means we need a much wider spawn area to the left
            this.x = Math.random() * (canvas.width + 600) - 400; 
            this.y = initial ? Math.random() * canvas.height : Math.random() * -100 - 50;
            
            const length = Math.random() * 20 + 10;
            const speed = Math.random() * 2 + 1; // Even slower: 1 to 3
            
            // Steeper Angle (Wind)
            const angle = 0.6; // Increased angle
            this.speedY = speed;
            this.speedX = speed * angle; 
            
            // Draw vector
            this.lenY = length;
            this.lenX = length * angle;

            this.opacity = Math.random() * 0.5 + 0.2;
            this.width = Math.random() * 2 + 1;
        }

        draw() {
            ctx.beginPath();
            ctx.lineWidth = this.width;
            ctx.strokeStyle = `rgba(135, 206, 250, ${this.opacity})`;
            ctx.lineCap = 'round';
            
            ctx.moveTo(this.x, this.y);
            ctx.lineTo(this.x + this.lenX, this.y + this.lenY);
            ctx.stroke();
        }

        update() {
            this.x += this.speedX;
            this.y += this.speedY;
            
            // Reset if off screen
            if (this.y > canvas.height || this.x > canvas.width + 200) {
                this.reset();
            }
        }
    }

    // Initialize drops
    for (let i = 0; i < dropCount; i++) {
        drops.push(new Drop());
    }

    function animate() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        drops.forEach(drop => {
            drop.draw();
            drop.update();
        });

        requestAnimationFrame(animate);
    }

    // Resize handler
    window.addEventListener('resize', () => {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    });

    animate();
}
