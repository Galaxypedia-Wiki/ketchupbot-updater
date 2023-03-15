FROM node:16 as  ts-compiler
WORKDIR /app
COPY package*.json ./
COPY tsconfig*.json ./
RUN npm install
COPY ./src ./src
RUN npm run build

FROM node:16
WORKDIR /app
COPY package*.json ./
RUN npm install --production
COPY --from=ts-compiler /app/dist ./dist
COPY . ./
CMD [ "node", "./dist/index.js" ]