FROM node:20-alpine
WORKDIR /app
COPY package*.json .
COPY tsconfig.json .
COPY src ./src
RUN npm ci
RUN npx tsc

FROM node:20-alpine
ENV NODE_ENV production
WORKDIR /app
COPY package*.json .
COPY banner.txt .
RUN npm ci
COPY --from=0 /app/dist ./dist
CMD ["node", "dist/index.js"]
