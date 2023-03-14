import {
  CreateDateColumn,
  Entity,
  JoinColumn,
  ManyToOne,
  OneToMany,
  PrimaryGeneratedColumn,
  UpdateDateColumn,
} from 'typeorm';

import Customer from '@modules/customers/typeorm/entities/Customer';
import OrdersProducts from './OrdersProducts';

@Entity('orders')
class Order {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  //relaciona muitas orders para um customer
  @ManyToOne(() => Customer)
  //indica qual é a colunan que faz a relação entre as entidades
  @JoinColumn({ name: 'customer_id' })
  customer: Customer;

  //relaciona uma order com muitos products
  //onde order_products se relaciona com order na Entity OrderProducts
  //como há relação com vários OrdersProducts, usamos o objeto com atributo cacade como true
  @OneToMany(() => OrdersProducts, order_products => order_products.order, {
    cascade: true,
  })
  order_products: OrdersProducts[];

  @CreateDateColumn()
  created_at: Date;

  @UpdateDateColumn()
  updated_at: Date;
}

export default Order;
