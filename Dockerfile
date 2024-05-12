FROM node:20-alpine
WORKDIR /app
COPY package*.json .
COPY tsconfig.json .
RUN npm ci
COPY src ./src
RUN npm run build

FROM node:20-alpine
ENV NODE_ENV production
WORKDIR /app
COPY package*.json .
COPY banner.txt .
RUN npm ci
COPY --from=0 /app/dist ./dist
CMD ["npm", "run", "runfrombuild"]