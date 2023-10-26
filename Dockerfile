FROM node:lts

ENV NODE_ENV production

WORKDIR /app

COPY package*.json ./

RUN npm ci

RUN npm install -g typescript

COPY . .

RUN tsc

CMD ["node", "dist/index.js"]
