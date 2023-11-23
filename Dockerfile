FROM node:20
WORKDIR /app
COPY package*.json .
COPY tsconfig.json .
COPY src ./src
RUN npm install
RUN npx tsc

FROM node:20
ENV NODE_ENV production
WORKDIR /app
COPY package*.json .
COPY banner.txt .
RUN npm ci
COPY --from=0 /app/dist ./dist
CMD ["node", "dist/index.js"]
