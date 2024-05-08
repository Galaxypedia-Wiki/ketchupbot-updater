/* eslint-disable @typescript-eslint/naming-convention */
import Bot from "nodemw";
import { promisify } from "util";

/**
 * NodeMW Promisified
 *
 * Wrapper for the NodeMW library which uses promises instead of callbacks
 */
export default class NodeMWP extends Bot {
    public getArticle = promisify(super.getArticle.bind(this));

    public edit = promisify(super.edit.bind(this));

    public parse = promisify(super.parse.bind(this));

    public getArticleRevisions = promisify(
        super.getArticleRevisions.bind(this),
    );

    public logIn = promisify(super.logIn.bind(this));

    public getPagesInCategory = promisify(super.getPagesInCategory.bind(this));
}
