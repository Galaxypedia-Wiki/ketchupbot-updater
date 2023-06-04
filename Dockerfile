FROM node:18
WORKDIR /app
COPY package*.json ./
RUN npm install --omit=dev && npm install -g typescript
COPY . .
RUN tsc
CMD ["node", "dist/index.js"]