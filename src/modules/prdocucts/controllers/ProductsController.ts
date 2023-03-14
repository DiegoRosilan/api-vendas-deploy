import { Request, Response } from 'express';
import CreateProductService from '../services/CreateProductService';
import DeleteProductService from '../services/DeleteProductService';
import ListProductService from '../services/ListProductService';
import ShowProductService from '../services/ShowProductService';
import UpdateProductService from '../services/UpdateProductService';

export default class ProductsController {
  //método para listagem
  public async index(request: Request, response: Response) {
    //instancia do servico ListProductsService
    const listProducts = new ListProductService();

    //executa o método execute do service ListProducts
    const products = await listProducts.execute();

    //retorna um json
    return response.json(products);
  }

  public async show(request: Request, response: Response) {
    //variável pra receber os parametros do request
    const { id } = request.params;
    //instancia do servico ShowProductsService
    const showPrdocuts = new ShowProductService();

    //executa o método execute do service ShowProducts
    const product = await showPrdocuts.execute({ id });

    //retorna um json
    return response.json(product);
  }

  public async create(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { name, price, quantity } = request.body;

    //instancia do servico CreateProductsService
    const createPrdocut = new CreateProductService();

    //executa o método execute do service CreateProducts
    const product = await createPrdocut.execute({
      name,
      price,
      quantity,
    });

    //retorna um json
    return response.json(product);
  }

  public async update(request: Request, response: Response) {
    //variáveis pra receber os parametros do body
    const { name, price, quantity } = request.body;
    //variável pra receber os parametros do request
    const { id } = request.params;

    //instancia do servico UpdateProductsService
    const updatePrdocut = new UpdateProductService();

    //executa o método execute do service UpdateProducts
    const product = await updatePrdocut.execute({
      id,
      name,
      price,
      quantity,
    });

    //retorna um json
    return response.json(product);
  }

  public async delete(request: Request, response: Response) {
    //variável pra receber os parametros do request
    const { id } = request.params;

    //instancia do servico DeleteProductsService
    const deletePrdocuts = new DeleteProductService();

    //executa o método execute do service DeleteProducts
    await deletePrdocuts.execute({ id });

    //retorna um json
    return response.json([]);
  }
}
